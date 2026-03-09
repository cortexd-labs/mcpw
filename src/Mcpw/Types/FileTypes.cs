using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record FileMetadata
{
    [JsonPropertyName("path")]            public string Path { get; init; } = "";
    [JsonPropertyName("size_bytes")]      public long SizeBytes { get; init; }
    [JsonPropertyName("created")]         public string Created { get; init; } = "";
    [JsonPropertyName("modified")]        public string Modified { get; init; } = "";
    [JsonPropertyName("accessed")]        public string Accessed { get; init; } = "";
    [JsonPropertyName("attributes")]      public string Attributes { get; init; } = "";
    [JsonPropertyName("is_directory")]    public bool IsDirectory { get; init; }
    [JsonPropertyName("owner")]           public string Owner { get; init; } = "";
}
