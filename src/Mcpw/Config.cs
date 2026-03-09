using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcpw;

public sealed record McpwConfig
{
    [JsonPropertyName("allowedPaths")]    public IReadOnlyList<string> AllowedPaths { get; init; } = [];
    [JsonPropertyName("blockedPaths")]    public IReadOnlyList<string> BlockedPaths { get; init; } = [];
    [JsonPropertyName("enabledDomains")]  public IReadOnlyList<string> EnabledDomains { get; init; } = DefaultEnabledDomains;
    [JsonPropertyName("disabledDomains")] public IReadOnlyList<string> DisabledDomains { get; init; } = [];
    [JsonPropertyName("privilegeTier")]   public string PrivilegeTier { get; init; } = "read";

    public static readonly IReadOnlyList<string> DefaultEnabledDomains =
    [
        "system", "process", "service", "log",
        "network", "file", "storage", "security",
        "container", "hardware", "schedule",
        "registry", "iis",
    ];

    public bool IsDomainEnabled(string domain)
    {
        if (DisabledDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
            return false;
        if (EnabledDomains.Count == 0)
            return true;
        return EnabledDomains.Contains(domain, StringComparer.OrdinalIgnoreCase);
    }
}

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        AllowTrailingCommas         = true,
    };

    public static McpwConfig Load(string? path = null)
    {
        path ??= @"C:\ProgramData\mcpw\config.json";

        if (!File.Exists(path))
            return new McpwConfig();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<McpwConfig>(json, Options) ?? new McpwConfig();
    }
}
