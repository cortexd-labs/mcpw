using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class StorageTools : IToolHandler
{
    private readonly IWmiClient _wmi;

    public StorageTools(IWmiClient wmi) => _wmi = wmi;

    public string Domain => "storage";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("storage.disks",  "List physical disks",              PrivilegeTier.Read, "{}"),
        Tool("storage.mounts", "List volumes and mount points",    PrivilegeTier.Read, "{}"),
        Tool("storage.usage",  "Drive space usage",                PrivilegeTier.Read, "{}"),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "storage.disks"  => Disks(),
            "storage.mounts" => Mounts(),
            "storage.usage"  => Usage(),
            _                => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult Disks()
    {
        var disks = _wmi.Query("SELECT DeviceID, Model, Size, Partitions, InterfaceType, SerialNumber FROM Win32_DiskDrive")
            .Select(r => new DiskInfo
            {
                DeviceId   = r["DeviceID"]?.ToString()    ?? "",
                Model      = r["Model"]?.ToString()       ?? "",
                SizeBytes  = Convert.ToInt64(r["Size"]   ?? 0L),
                Partitions = Convert.ToInt32(r["Partitions"] ?? 0),
                Interface  = r["InterfaceType"]?.ToString() ?? "",
                Serial     = r["SerialNumber"]?.ToString()  ?? "",
            })
            .ToList();
        return McpJson.JsonResult(disks);
    }

    private McpCallToolResult Mounts()
    {
        var volumes = _wmi.Query("SELECT Name, Label, DriveLetter, FileSystem, Capacity, FreeSpace FROM Win32_Volume")
            .Select(r => new VolumeInfo
            {
                Name          = r["Name"]?.ToString()        ?? "",
                Label         = r["Label"]?.ToString()       ?? "",
                DriveLetter   = r["DriveLetter"]?.ToString() ?? "",
                FileSystem    = r["FileSystem"]?.ToString()  ?? "",
                CapacityBytes = Convert.ToInt64(r["Capacity"]  ?? 0L),
                FreeBytes     = Convert.ToInt64(r["FreeSpace"] ?? 0L),
            })
            .ToList();
        return McpJson.JsonResult(volumes);
    }

    private McpCallToolResult Usage()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new VolumeInfo
            {
                Name          = d.Name,
                Label         = d.VolumeLabel,
                FileSystem    = d.DriveFormat,
                CapacityBytes = d.TotalSize,
                FreeBytes     = d.AvailableFreeSpace,
            })
            .ToList();
        return McpJson.JsonResult(drives);
    }

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
