using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record LocalUser
{
    [JsonPropertyName("name")]        public string Name { get; init; } = "";
    [JsonPropertyName("full_name")]   public string FullName { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("enabled")]     public bool Enabled { get; init; }
    [JsonPropertyName("local_account")]public bool LocalAccount { get; init; }
    [JsonPropertyName("sid")]         public string Sid { get; init; } = "";
}

public sealed record LocalGroup
{
    [JsonPropertyName("name")]        public string Name { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("sid")]         public string Sid { get; init; } = "";
}

public sealed record WhoAmI
{
    [JsonPropertyName("name")]              public string Name { get; init; } = "";
    [JsonPropertyName("sid")]               public string Sid { get; init; } = "";
    [JsonPropertyName("authentication_type")]public string AuthenticationType { get; init; } = "";
    [JsonPropertyName("is_system")]         public bool IsSystem { get; init; }
    [JsonPropertyName("groups")]            public IReadOnlyList<string> Groups { get; init; } = [];
}
