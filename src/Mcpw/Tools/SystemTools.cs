using System.Runtime.InteropServices;
using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class SystemTools : IToolHandler
{
    private readonly IWmiClient _wmi;

    public SystemTools(IWmiClient wmi) => _wmi = wmi;

    public string Domain => "system";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("system.info",   "OS version, hostname, architecture, uptime, domain",  PrivilegeTier.Read,   "{}"),
        Tool("system.uptime", "System uptime in seconds",                             PrivilegeTier.Read,   "{}"),
        Tool("system.env",    "List or get environment variables",                    PrivilegeTier.Read,
            """{"type":"object","properties":{"name":{"type":"string","description":"Variable name to filter (optional)"}}}"""),
        Tool("system.reboot", "Reboot or shutdown the system",                        PrivilegeTier.Dangerous,
            """{"type":"object","properties":{"action":{"type":"string","enum":["reboot","shutdown"],"default":"reboot"},"delay_seconds":{"type":"integer","default":0}}}"""),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "system.info"   => SystemInfo(),
            "system.uptime" => Uptime(),
            "system.env"    => Env(args),
            "system.reboot" => Reboot(args),
            _               => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    // ── implementations ───────────────────────────────────────────────────

    private McpCallToolResult SystemInfo()
    {
        var rows = _wmi.Query("SELECT Caption, Version, CSName, OSArchitecture, LastBootUpTime, NumberOfProcessors, Domain FROM Win32_OperatingSystem")
            .FirstOrDefault();

        var info = new SystemInfo
        {
            Hostname       = rows?["CSName"]?.ToString()          ?? Environment.MachineName,
            OsName         = rows?["Caption"]?.ToString()         ?? RuntimeInformation.OSDescription,
            OsVersion      = rows?["Version"]?.ToString()         ?? "",
            Architecture   = rows?["OSArchitecture"]?.ToString()  ?? RuntimeInformation.OSArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            UptimeSeconds  = Environment.TickCount64 / 1000,
            Domain         = rows?["Domain"]?.ToString()          ?? "",
        };

        return McpJson.JsonResult(info);
    }

    private McpCallToolResult Uptime()
    {
        var seconds = Environment.TickCount64 / 1000;
        return McpJson.TextResult($"{seconds}");
    }

    private McpCallToolResult Env(JsonElement? args)
    {
        var nameFilter = args?.TryGetProperty("name", out var n) == true ? n.GetString() : null;

        var vars = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Select(e => new EnvVar { Name = e.Key.ToString()!, Value = e.Value?.ToString() ?? "" })
            .Where(v => nameFilter is null || v.Name.Equals(nameFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v.Name)
            .ToList();

        return McpJson.JsonResult(vars);
    }

    private McpCallToolResult Reboot(JsonElement? args)
    {
        var action = args?.TryGetProperty("action", out var a) == true
            ? a.GetString() ?? "reboot"
            : "reboot";
        var delay = args?.TryGetProperty("delay_seconds", out var d) == true
            ? d.GetInt32()
            : 0;

        var flag  = action == "shutdown" ? "/s" : "/r";
        System.Diagnostics.Process.Start("shutdown.exe", $"{flag} /t {delay}");
        return McpJson.TextResult($"System {action} initiated with {delay}s delay.");
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
