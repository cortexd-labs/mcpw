using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record ScheduledTask
{
    [JsonPropertyName("name")]          public string Name { get; init; } = "";
    [JsonPropertyName("path")]          public string Path { get; init; } = "";
    [JsonPropertyName("state")]         public string State { get; init; } = "";
    [JsonPropertyName("last_run_time")] public string LastRunTime { get; init; } = "";
    [JsonPropertyName("next_run_time")] public string NextRunTime { get; init; } = "";
    [JsonPropertyName("last_result")]   public int LastResult { get; init; }
    [JsonPropertyName("author")]        public string Author { get; init; } = "";
    [JsonPropertyName("description")]   public string Description { get; init; } = "";
}
