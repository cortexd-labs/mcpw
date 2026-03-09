using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record ContainerInfo
{
    [JsonPropertyName("id")]      public string Id { get; init; } = "";
    [JsonPropertyName("name")]    public string Name { get; init; } = "";
    [JsonPropertyName("image")]   public string Image { get; init; } = "";
    [JsonPropertyName("status")]  public string Status { get; init; } = "";
    [JsonPropertyName("state")]   public string State { get; init; } = "";
    [JsonPropertyName("created")] public string Created { get; init; } = "";
    [JsonPropertyName("ports")]   public IReadOnlyList<string> Ports { get; init; } = [];
}
