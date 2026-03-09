using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class ServiceTools : IToolHandler
{
    private readonly IServiceControl _svc;
    private readonly IEventLogAccess _log;

    public ServiceTools(IServiceControl svc, IEventLogAccess log)
    {
        _svc = svc;
        _log = log;
    }

    public string Domain => "service";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("service.list",    "List all Windows services and their status",         PrivilegeTier.Read,    "{}"),
        Tool("service.status",  "Status of a specific service",                       PrivilegeTier.Read,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("service.start",   "Start a Windows service",                            PrivilegeTier.Operate,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("service.stop",    "Stop a Windows service",                             PrivilegeTier.Operate,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("service.restart", "Restart a Windows service",                          PrivilegeTier.Operate,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("service.logs",    "Recent event log entries for a service",             PrivilegeTier.Read,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"count":{"type":"integer","default":50}}}"""),
        Tool("service.enable",  "Set service startup type (auto/manual/disabled)",   PrivilegeTier.Operate,
            """{"type":"object","required":["name","start_type"],"properties":{"name":{"type":"string"},"start_type":{"type":"string","enum":["auto","manual","disabled"]}}}"""),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "service.list"    => McpJson.JsonResult(_svc.GetServices().ToList()),
            "service.status"  => ServiceStatus(args),
            "service.start"   => ServiceOp(args, _svc.Start,   "started"),
            "service.stop"    => ServiceOp(args, _svc.Stop,    "stopped"),
            "service.restart" => ServiceOp(args, _svc.Restart, "restarted"),
            "service.logs"    => ServiceLogs(args),
            "service.enable"  => ServiceEnable(args),
            _                 => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    // ── implementations ───────────────────────────────────────────────────

    private McpCallToolResult ServiceStatus(JsonElement? args)
    {
        var name = RequiredString(args, "name");
        return name is null
            ? McpJson.ErrorResult("Missing required argument: name")
            : McpJson.JsonResult(_svc.GetService(name));
    }

    private McpCallToolResult ServiceOp(JsonElement? args, Action<string> op, string verb)
    {
        var name = RequiredString(args, "name");
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        InputValidator.AssertNoInjection(name, "name");
        op(name);
        return McpJson.TextResult($"Service '{name}' {verb}.");
    }

    private McpCallToolResult ServiceLogs(JsonElement? args)
    {
        var name  = RequiredString(args, "name");
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        var count = args?.TryGetProperty("count", out var c) == true ? c.GetInt32() : 50;
        var logs  = _log.GetEntries("System", count, sourceFilter: name).ToList();
        return McpJson.JsonResult(logs);
    }

    private McpCallToolResult ServiceEnable(JsonElement? args)
    {
        var name      = RequiredString(args, "name");
        var startType = RequiredString(args, "start_type");
        if (name is null || startType is null)
            return McpJson.ErrorResult("Missing required arguments: name, start_type");
        InputValidator.AssertNoInjection(name, "name");
        _svc.SetStartType(name, startType);
        return McpJson.TextResult($"Service '{name}' start type set to '{startType}'.");
    }

    private static string? RequiredString(JsonElement? args, string key) =>
        args?.TryGetProperty(key, out var v) == true ? v.GetString() : null;

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
