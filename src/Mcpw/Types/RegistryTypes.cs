using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record RegistryValue
{
    [JsonPropertyName("name")]  public string Name { get; init; } = "";
    [JsonPropertyName("type")]  public string Type { get; init; } = "";
    [JsonPropertyName("value")] public string Value { get; init; } = "";
}

public sealed record RegistryKeyListing
{
    [JsonPropertyName("path")]      public string Path { get; init; } = "";
    [JsonPropertyName("subkeys")]   public IReadOnlyList<string> Subkeys { get; init; } = [];
    [JsonPropertyName("values")]    public IReadOnlyList<RegistryValue> Values { get; init; } = [];
}
