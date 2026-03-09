using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class LogTools : IToolHandler
{
    private readonly IEventLogAccess _log;

    public LogTools(IEventLogAccess log) => _log = log;

    public string Domain => "log";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("log.tail",  "Recent entries from an event log channel", PrivilegeTier.Read,
            """{"type":"object","properties":{"log_name":{"type":"string","default":"System"},"count":{"type":"integer","default":50}}}"""),
        Tool("log.search","Search event logs by keyword, level, time range", PrivilegeTier.Read,
            """{"type":"object","properties":{"log_name":{"type":"string","default":"System"},"keyword":{"type":"string"},"level":{"type":"string","enum":["Critical","Error","Warning","Information","Verbose"]},"since":{"type":"string","description":"ISO 8601 timestamp"}}}"""),
        Tool("log.units", "List available event log channels", PrivilegeTier.Read, "{}"),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "log.tail"   => Tail(args),
            "log.search" => Search(args),
            "log.units"  => McpJson.JsonResult(_log.GetLogNames().OrderBy(n => n).ToList()),
            _            => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult Tail(JsonElement? args)
    {
        var logName = args?.TryGetProperty("log_name", out var ln) == true ? ln.GetString() ?? "System" : "System";
        var count   = args?.TryGetProperty("count",    out var c)  == true ? c.GetInt32()               : 50;
        return McpJson.JsonResult(_log.GetEntries(logName, count).ToList());
    }

    private McpCallToolResult Search(JsonElement? args)
    {
        var logName = args?.TryGetProperty("log_name", out var ln) == true ? ln.GetString() ?? "System" : "System";
        var keyword = args?.TryGetProperty("keyword",  out var kw) == true ? kw.GetString()             : null;
        var level   = args?.TryGetProperty("level",    out var lv) == true ? lv.GetString()             : null;
        DateTimeOffset? since = null;
        if (args?.TryGetProperty("since", out var s) == true && s.GetString() is string sinceStr)
            since = DateTimeOffset.TryParse(sinceStr, out var dt) ? dt : null;

        return McpJson.JsonResult(_log.Search(logName, keyword, level, since).ToList());
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
