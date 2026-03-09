using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record SystemInfo
{
    [JsonPropertyName("hostname")]        public string Hostname { get; init; } = "";
    [JsonPropertyName("os_name")]         public string OsName { get; init; } = "";
    [JsonPropertyName("os_version")]      public string OsVersion { get; init; } = "";
    [JsonPropertyName("architecture")]    public string Architecture { get; init; } = "";
    [JsonPropertyName("uptime_seconds")]  public long UptimeSeconds { get; init; }
    [JsonPropertyName("processor_count")] public int ProcessorCount { get; init; }
    [JsonPropertyName("domain")]          public string Domain { get; init; } = "";
}

public sealed record EnvVar
{
    [JsonPropertyName("name")]  public string Name { get; init; } = "";
    [JsonPropertyName("value")] public string Value { get; init; } = "";
}
