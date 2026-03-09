using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record NetworkInterface
{
    [JsonPropertyName("name")]            public string Name { get; init; } = "";
    [JsonPropertyName("description")]     public string Description { get; init; } = "";
    [JsonPropertyName("mac_address")]     public string MacAddress { get; init; } = "";
    [JsonPropertyName("status")]          public string Status { get; init; } = "";
    [JsonPropertyName("ipv4_addresses")]  public IReadOnlyList<string> Ipv4Addresses { get; init; } = [];
    [JsonPropertyName("ipv6_addresses")]  public IReadOnlyList<string> Ipv6Addresses { get; init; } = [];
    [JsonPropertyName("speed_mbps")]      public long SpeedMbps { get; init; }
}

public sealed record TcpEndpoint
{
    [JsonPropertyName("address")] public string Address { get; init; } = "";
    [JsonPropertyName("port")]    public int Port { get; init; }
}

public sealed record TcpConnection
{
    [JsonPropertyName("local")]  public TcpEndpoint Local { get; init; } = new();
    [JsonPropertyName("remote")] public TcpEndpoint Remote { get; init; } = new();
    [JsonPropertyName("state")]  public string State { get; init; } = "";
}

public sealed record FirewallRule
{
    [JsonPropertyName("name")]      public string Name { get; init; } = "";
    [JsonPropertyName("direction")] public string Direction { get; init; } = "";
    [JsonPropertyName("action")]    public string Action { get; init; } = "";
    [JsonPropertyName("protocol")]  public string Protocol { get; init; } = "";
    [JsonPropertyName("enabled")]   public bool Enabled { get; init; }
}
