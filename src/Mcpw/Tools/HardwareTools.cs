using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class HardwareTools : IToolHandler
{
    private readonly IWmiClient _wmi;

    public HardwareTools(IWmiClient wmi) => _wmi = wmi;

    public string Domain => "hardware";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("hardware.pci", "List PCI/PCIe devices via WMI PnP", PrivilegeTier.Read, "{}"),
        Tool("hardware.usb", "List USB devices via WMI",          PrivilegeTier.Read, "{}"),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "hardware.pci" => PciDevices(),
            "hardware.usb" => UsbDevices(),
            _              => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult PciDevices()
    {
        var devices = _wmi.Query(
            "SELECT DeviceID, Name, Manufacturer, PNPClass, Status FROM Win32_PnPEntity WHERE PNPClass IS NOT NULL")
            .Select(r => new PciDevice
            {
                DeviceId     = r["DeviceID"]?.ToString()     ?? "",
                Name         = r["Name"]?.ToString()         ?? "",
                Manufacturer = r["Manufacturer"]?.ToString() ?? "",
                Class        = r["PNPClass"]?.ToString()     ?? "",
                Status       = r["Status"]?.ToString()       ?? "",
            })
            .Where(d => d.DeviceId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return McpJson.JsonResult(devices);
    }

    private McpCallToolResult UsbDevices()
    {
        var devices = _wmi.Query(
            "SELECT DeviceID, Name, Manufacturer, Status FROM Win32_PnPEntity")
            .Where(r => r["DeviceID"]?.ToString()?.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) == true)
            .Select(r => new UsbDevice
            {
                DeviceId     = r["DeviceID"]?.ToString()     ?? "",
                Name         = r["Name"]?.ToString()         ?? "",
                Manufacturer = r["Manufacturer"]?.ToString() ?? "",
                Status       = r["Status"]?.ToString()       ?? "",
            })
            .ToList();
        return McpJson.JsonResult(devices);
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
