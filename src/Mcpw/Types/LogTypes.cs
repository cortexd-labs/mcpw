using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record LogEntry
{
    [JsonPropertyName("time_generated")] public string TimeGenerated { get; init; } = "";
    [JsonPropertyName("level")]          public string Level { get; init; } = "";
    [JsonPropertyName("source")]         public string Source { get; init; } = "";
    [JsonPropertyName("event_id")]       public long EventId { get; init; }
    [JsonPropertyName("message")]        public string Message { get; init; } = "";
    [JsonPropertyName("log_name")]       public string LogName { get; init; } = "";
}
