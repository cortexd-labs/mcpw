using System.Text.Json;
using Mcpw.Types;
using Mcpw.Windows;

namespace Mcpw.Tools;

public sealed class RegistryTools : IToolHandler
{
    private readonly IRegistryAccess _registry;

    public RegistryTools(IRegistryAccess registry) => _registry = registry;

    public string Domain => "registry";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("registry.get",    "Read a registry value",                           PrivilegeTier.Read,
            """{"type":"object","required":["hive","key","name"],"properties":{"hive":{"type":"string","description":"HKLM, HKCU, HKCR, HKU, HKCC"},"key":{"type":"string"},"name":{"type":"string"}}}"""),
        Tool("registry.set",    "Write a registry value",                          PrivilegeTier.Dangerous,
            """{"type":"object","required":["hive","key","name","value"],"properties":{"hive":{"type":"string"},"key":{"type":"string"},"name":{"type":"string"},"value":{"type":"string"},"kind":{"type":"string","default":"String","enum":["String","ExpandString","Binary","DWord","MultiString","QWord"]}}}"""),
        Tool("registry.delete", "Delete a registry key or value",                  PrivilegeTier.Dangerous,
            """{"type":"object","required":["hive","key"],"properties":{"hive":{"type":"string"},"key":{"type":"string"},"name":{"type":"string","description":"Value name to delete. Omit to delete the key itself."},"recursive":{"type":"boolean","default":false}}}"""),
        Tool("registry.list",   "List subkeys and values under a registry key",    PrivilegeTier.Read,
            """{"type":"object","required":["hive","key"],"properties":{"hive":{"type":"string"},"key":{"type":"string"}}}"""),
    ];

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        var result = toolName switch
        {
            "registry.get"    => RegistryGet(args),
            "registry.set"    => RegistrySet(args),
            "registry.delete" => RegistryDelete(args),
            "registry.list"   => RegistryList(args),
            _                 => McpJson.ErrorResult($"Unknown tool: {toolName}"),
        };
        return Task.FromResult(result);
    }

    private McpCallToolResult RegistryGet(JsonElement? args)
    {
        if (!TryGetHiveKey(args, out var hive, out var key)) return McpJson.ErrorResult("Missing hive or key");
        var name = RequiredString(args, "name");
        if (name is null) return McpJson.ErrorResult("Missing required argument: name");
        var value = _registry.GetValue(hive, key, name);
        return McpJson.JsonResult(new RegistryValue { Name = name, Value = value?.ToString() ?? "" });
    }

    private McpCallToolResult RegistrySet(JsonElement? args)
    {
        if (!TryGetHiveKey(args, out var hive, out var key)) return McpJson.ErrorResult("Missing hive or key");
        var name  = RequiredString(args, "name");
        var value = RequiredString(args, "value");
        var kind  = args?.TryGetProperty("kind", out var k) == true ? k.GetString() ?? "String" : "String";
        if (name is null || value is null) return McpJson.ErrorResult("Missing required arguments: name, value");
        _registry.SetValue(hive, key, name, value, kind);
        return McpJson.TextResult($"Set {hive}\\{key}\\{name} = {value}");
    }

    private McpCallToolResult RegistryDelete(JsonElement? args)
    {
        if (!TryGetHiveKey(args, out var hive, out var key)) return McpJson.ErrorResult("Missing hive or key");
        var name      = args?.TryGetProperty("name",      out var n) == true ? n.GetString() : null;
        var recursive = args?.TryGetProperty("recursive", out var r) == true && r.GetBoolean();

        if (name is not null)
        {
            _registry.DeleteValue(hive, key, name);
            return McpJson.TextResult($"Deleted value '{name}' from {hive}\\{key}");
        }
        _registry.DeleteKey(hive, key, recursive);
        return McpJson.TextResult($"Deleted key {hive}\\{key}");
    }

    private McpCallToolResult RegistryList(JsonElement? args)
    {
        if (!TryGetHiveKey(args, out var hive, out var key)) return McpJson.ErrorResult("Missing hive or key");
        return McpJson.JsonResult(_registry.ListKey(hive, key));
    }

    private static bool TryGetHiveKey(JsonElement? args, out string hive, out string key)
    {
        hive = key = "";
        if (args is null) return false;
        if (args.Value.TryGetProperty("hive", out var h)) hive = h.GetString() ?? "";
        if (args.Value.TryGetProperty("key",  out var k)) key  = k.GetString() ?? "";
        return !string.IsNullOrEmpty(hive) && !string.IsNullOrEmpty(key);
    }

    private static string? RequiredString(JsonElement? args, string prop) =>
        args?.TryGetProperty(prop, out var v) == true ? v.GetString() : null;

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
