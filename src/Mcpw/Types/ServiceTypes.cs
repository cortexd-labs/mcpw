using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record ServiceInfo
{
    [JsonPropertyName("name")]         public string Name { get; init; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; init; } = "";
    [JsonPropertyName("status")]       public string Status { get; init; } = "";
    [JsonPropertyName("start_type")]   public string StartType { get; init; } = "";
    [JsonPropertyName("description")]  public string Description { get; init; } = "";
}
