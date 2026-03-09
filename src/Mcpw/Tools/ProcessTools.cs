using System.Diagnostics;
using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class ProcessTools : IToolHandler
{
    private readonly IWmiClient _wmi;

    public ProcessTools(IWmiClient wmi) => _wmi = wmi;

    public string Domain => "process";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("process.list",    "List all running processes",                          PrivilegeTier.Read,    "{}"),
        Tool("process.inspect", "Detailed info for a specific process",                PrivilegeTier.Read,
            """{"type":"object","required":["pid"],"properties":{"pid":{"type":"integer"}}}"""),
        Tool("process.kill",    "Terminate a process by PID",                          PrivilegeTier.Operate,
            """{"type":"object","required":["pid"],"properties":{"pid":{"type":"integer"},"force":{"type":"boolean","default":true}}}"""),
        Tool("process.top",     "CPU/memory sorted process list (top N)",              PrivilegeTier.Read,
            """{"type":"object","properties":{"limit":{"type":"integer","default":20},"sort_by":{"type":"string","enum":["cpu","memory"],"default":"cpu"}}}"""),
        Tool("process.tree",    "Process parent-child tree",                           PrivilegeTier.Read,    "{}"),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "process.list"    => ListProcesses(),
            "process.inspect" => InspectProcess(args),
            "process.kill"    => KillProcess(args),
            "process.top"     => TopProcesses(args),
            "process.tree"    => ProcessTree(),
            _                 => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    // ── implementations ───────────────────────────────────────────────────

    private McpCallToolResult ListProcesses()
    {
        var procs = Process.GetProcesses()
            .Select(p => SafeMap(p))
            .Where(p => p is not null)
            .Cast<ProcessInfo>()
            .OrderBy(p => p.Name)
            .ToList();

        return McpJson.JsonResult(procs);
    }

    private McpCallToolResult InspectProcess(JsonElement? args)
    {
        if (args is null || !args.Value.TryGetProperty("pid", out var pidEl))
            return McpJson.ErrorResult("Missing required argument: pid");

        var pid = pidEl.GetInt32();
        Process p;
        try { p = Process.GetProcessById(pid); }
        catch (ArgumentException) { return McpJson.ErrorResult($"No process with PID {pid}"); }

        using (p)
            return McpJson.JsonResult(SafeMap(p));
    }

    private McpCallToolResult KillProcess(JsonElement? args)
    {
        if (args is null || !args.Value.TryGetProperty("pid", out var pidEl))
            return McpJson.ErrorResult("Missing required argument: pid");

        var pid = pidEl.GetInt32();
        Process p;
        try { p = Process.GetProcessById(pid); }
        catch (ArgumentException) { return McpJson.ErrorResult($"No process with PID {pid}"); }

        using (p)
        {
            p.Kill(entireProcessTree: true);
            return McpJson.TextResult($"Process {pid} ({p.ProcessName}) terminated.");
        }
    }

    private McpCallToolResult TopProcesses(JsonElement? args)
    {
        var limit  = args?.TryGetProperty("limit", out var l)  == true ? l.GetInt32()    : 20;
        var sortBy = args?.TryGetProperty("sort_by", out var s) == true ? s.GetString()   : "cpu";

        var all = Process.GetProcesses()
            .Select(p => SafeMap(p))
            .Where(p => p is not null)
            .Cast<ProcessInfo>();

        var sorted = sortBy == "memory"
            ? all.OrderByDescending(p => p.MemoryBytes)
            : all.OrderByDescending(p => p.CpuPercent);

        return McpJson.JsonResult(sorted.Take(limit).ToList());
    }

    private McpCallToolResult ProcessTree()
    {
        var rows = _wmi.Query("SELECT ProcessId, ParentProcessId, Name FROM Win32_Process");
        var byId = rows.ToDictionary(
            r => Convert.ToInt32(r["ProcessId"]),
            r => (name: r["Name"]?.ToString() ?? "", parent: Convert.ToInt32(r["ParentProcessId"])));

        var children = new Dictionary<int, List<int>>();
        foreach (var (pid, (_, parent)) in byId)
        {
            if (!children.ContainsKey(parent)) children[parent] = [];
            children[parent].Add(pid);
        }

        ProcessTreeNode Build(int pid) => new()
        {
            Pid      = pid,
            Name     = byId.TryGetValue(pid, out var v) ? v.name : "?",
            Children = children.TryGetValue(pid, out var kids)
                ? kids.Select(Build).ToList()
                : [],
        };

        // Roots = processes whose parent isn't in the set
        var roots = byId.Keys
            .Where(pid => !byId.ContainsKey(byId[pid].parent))
            .Select(Build)
            .ToList();

        return McpJson.JsonResult(roots);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static ProcessInfo? SafeMap(Process p)
    {
        try
        {
            return new ProcessInfo
            {
                Pid            = p.Id,
                Name           = p.ProcessName,
                MemoryBytes    = p.WorkingSet64,
                ThreadCount    = p.Threads.Count,
                StartTime      = SafeStartTime(p),
                ExecutablePath = SafeMainModule(p),
            };
        }
        catch { return null; }
    }

    private static string SafeStartTime(Process p)
    {
        try { return p.StartTime.ToString("o"); } catch { return ""; }
    }

    private static string SafeMainModule(Process p)
    {
        try { return p.MainModule?.FileName ?? ""; } catch { return ""; }
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
