using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record IISSite
{
    [JsonPropertyName("id")]       public long Id { get; init; }
    [JsonPropertyName("name")]     public string Name { get; init; } = "";
    [JsonPropertyName("state")]    public string State { get; init; } = "";
    [JsonPropertyName("bindings")] public IReadOnlyList<string> Bindings { get; init; } = [];
    [JsonPropertyName("physical_path")] public string PhysicalPath { get; init; } = "";
}

public sealed record IISAppPool
{
    [JsonPropertyName("name")]            public string Name { get; init; } = "";
    [JsonPropertyName("state")]           public string State { get; init; } = "";
    [JsonPropertyName("runtime_version")] public string RuntimeVersion { get; init; } = "";
    [JsonPropertyName("pipeline_mode")]   public string PipelineMode { get; init; } = "";
    [JsonPropertyName("identity_type")]   public string IdentityType { get; init; } = "";
    [JsonPropertyName("auto_start")]      public bool AutoStart { get; init; }
}
