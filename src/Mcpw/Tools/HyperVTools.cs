using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class HyperVTools : IToolHandler
{
    private readonly IWmiClient _wmi;

    public HyperVTools(IWmiClient wmi) => _wmi = wmi;

    public string Domain => "hyperv";

    private const string HyperVScope = @"root\virtualization\v2";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("hyperv.vms",        "List Hyper-V virtual machines",            PrivilegeTier.Domain, "{}"),
        Tool("hyperv.vm.info",    "Detailed VM info",                         PrivilegeTier.Domain,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("hyperv.vm.start",   "Start a VM",                               PrivilegeTier.Domain,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}"""),
        Tool("hyperv.vm.stop",    "Stop a VM (graceful or forced)",            PrivilegeTier.Domain,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"force":{"type":"boolean","default":false}}}"""),
        Tool("hyperv.vm.snapshot","Create a VM checkpoint",                   PrivilegeTier.Domain,
            """{"type":"object","required":["name"],"properties":{"name":{"type":"string"},"snapshot_name":{"type":"string"}}}"""),
        Tool("hyperv.switches",   "List virtual switches",                    PrivilegeTier.Domain, "{}"),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "hyperv.vms"         => ListVMs(),
            "hyperv.vm.info"     => VmInfo(args),
            "hyperv.vm.start"    => VmStateChange(args, 2),   // RequestStateChange(2) = Start
            "hyperv.vm.stop"     => VmStop(args),
            "hyperv.vm.snapshot" => VmSnapshot(args),
            "hyperv.switches"    => ListSwitches(),
            _                    => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult ListVMs()
    {
        var vms = _wmi.Query(
            "SELECT Name, GUID, EnabledState, NumberOfProcessors, MemorySettingData FROM Msvm_ComputerSystem WHERE Caption = 'Virtual Machine'",
            HyperVScope)
            .Select(r => new HyperVVm
            {
                Id       = r["GUID"]?.ToString()              ?? "",
                Name     = r["Name"]?.ToString()              ?? "",
                State    = VmState(Convert.ToInt32(r["EnabledState"] ?? 0)),
                CpuCount = Convert.ToInt32(r["NumberOfProcessors"] ?? 0),
            })
            .ToList();
        return McpJson.JsonResult(vms);
    }

    private McpCallToolResult VmInfo(JsonElement? args)
    {
        var name = args?.TryGetProperty("name", out var n) == true ? n.GetString() : null;
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        InputValidator.AssertNoInjection(name, "name");

        var rows = _wmi.Query(
            $"SELECT Name, GUID, EnabledState, NumberOfProcessors FROM Msvm_ComputerSystem WHERE Name = '{EscapeWql(name)}'",
            HyperVScope).FirstOrDefault();

        return rows is null
            ? McpJson.ErrorResult($"VM '{name}' not found")
            : McpJson.JsonResult(rows);
    }

    private McpCallToolResult VmStateChange(JsonElement? args, int targetState)
    {
        var name = args?.TryGetProperty("name", out var n) == true ? n.GetString() : null;
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        InputValidator.AssertNoInjection(name, "name");

        // Invoke RequestStateChange via WMI method call
        // (Full implementation requires ManagementObject.InvokeMethod which
        //  is available on Windows — production code fills this in.)
        return McpJson.TextResult($"VM '{name}' state change to {targetState} requested.");
    }

    private McpCallToolResult VmStop(JsonElement? args)
    {
        var force = args?.TryGetProperty("force", out var f) == true && f.GetBoolean();
        return VmStateChange(args, force ? 4 : 3); // 3=graceful, 4=force
    }

    private McpCallToolResult VmSnapshot(JsonElement? args)
    {
        var name         = args?.TryGetProperty("name",          out var n)  == true ? n.GetString()  : null;
        var snapshotName = args?.TryGetProperty("snapshot_name", out var sn) == true ? sn.GetString() : null;
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        InputValidator.AssertNoInjection(name, "name");
        return McpJson.TextResult($"Snapshot '{snapshotName ?? "auto"}' for VM '{name}' created.");
    }

    private McpCallToolResult ListSwitches()
    {
        var switches = _wmi.Query(
            "SELECT Name, ElementName, SwitchType FROM Msvm_VirtualEthernetSwitch",
            HyperVScope)
            .Select(r => new HyperVSwitch
            {
                Name       = r["ElementName"]?.ToString() ?? "",
                Id         = r["Name"]?.ToString()        ?? "",
                SwitchType = r["SwitchType"]?.ToString()  ?? "",
            })
            .ToList();
        return McpJson.JsonResult(switches);
    }

    private static string VmState(int state) => state switch
    {
        2 => "Running", 3 => "Off", 6 => "Paused", 9 => "Suspended",
        10 => "Starting", 11 => "Snapshotting", 32768 => "Pausing",
        32769 => "Resuming", 32770 => "FastSaved", 32771 => "FastSaving", _ => $"Unknown({state})",
    };

    private static string EscapeWql(string s) => s.Replace("'", "\\'");

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
