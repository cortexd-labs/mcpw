using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record HyperVVm
{
    [JsonPropertyName("id")]                 public string Id { get; init; } = "";
    [JsonPropertyName("name")]               public string Name { get; init; } = "";
    [JsonPropertyName("state")]              public string State { get; init; } = "";
    [JsonPropertyName("cpu_count")]          public int CpuCount { get; init; }
    [JsonPropertyName("memory_mb")]          public long MemoryMb { get; init; }
    [JsonPropertyName("generation")]         public int Generation { get; init; }
    [JsonPropertyName("uptime")]             public string Uptime { get; init; } = "";
}

public sealed record HyperVSwitch
{
    [JsonPropertyName("name")]               public string Name { get; init; } = "";
    [JsonPropertyName("id")]                 public string Id { get; init; } = "";
    [JsonPropertyName("switch_type")]        public string SwitchType { get; init; } = "";
}
