using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class ScheduleTools : IToolHandler
{
    private readonly IPowerShellHost _ps;

    public ScheduleTools(IPowerShellHost ps) => _ps = ps;

    public string Domain => "schedule";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("schedule.list",   "List all scheduled tasks",          PrivilegeTier.Read,    "{}"),
        Tool("schedule.remove", "Delete a scheduled task by path",   PrivilegeTier.Operate,
            """{"type":"object","required":["path"],"properties":{"path":{"type":"string","description":"Task path, e.g. \\MyTask or \\Folder\\MyTask"}}}"""),
    ];

    public async Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        return toolName switch
        {
            "schedule.list"   => await ListTasks(ct),
            "schedule.remove" => await RemoveTask(args, ct),
            _                 => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
    }

    private async Task<McpCallToolResult> ListTasks(CancellationToken ct)
    {
        var json = await _ps.RunJsonAsync(
            "Get-ScheduledTask | Select-Object TaskName,TaskPath,State,Author,Description | ForEach-Object { $_ | Add-Member -NotePropertyName LastRunTime -NotePropertyValue (($_ | Get-ScheduledTaskInfo).LastRunTime) -PassThru }",
            ct);
        return McpJson.TextResult(json);
    }

    private async Task<McpCallToolResult> RemoveTask(JsonElement? args, CancellationToken ct)
    {
        var path = args?.TryGetProperty("path", out var p) == true ? p.GetString() : null;
        if (path is null) return McpJson.ErrorResult("Missing required argument: path");
        InputValidator.AssertNoInjection(path, "path");

        await _ps.RunAsync($"Unregister-ScheduledTask -TaskPath '{EscapePs(path)}' -Confirm:$false", ct);
        return McpJson.TextResult($"Scheduled task '{path}' removed.");
    }

    private static string EscapePs(string s) => s.Replace("'", "''");

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
