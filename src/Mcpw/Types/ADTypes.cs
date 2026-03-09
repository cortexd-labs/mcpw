using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record AdUser
{
    [JsonPropertyName("sam_account_name")]   public string SamAccountName { get; init; } = "";
    [JsonPropertyName("display_name")]       public string DisplayName { get; init; } = "";
    [JsonPropertyName("email")]              public string Email { get; init; } = "";
    [JsonPropertyName("dn")]                 public string Dn { get; init; } = "";
    [JsonPropertyName("enabled")]            public bool Enabled { get; init; }
    [JsonPropertyName("locked_out")]         public bool LockedOut { get; init; }
    [JsonPropertyName("department")]         public string Department { get; init; } = "";
    [JsonPropertyName("title")]              public string Title { get; init; } = "";
}

public sealed record AdGroup
{
    [JsonPropertyName("name")]               public string Name { get; init; } = "";
    [JsonPropertyName("dn")]                 public string Dn { get; init; } = "";
    [JsonPropertyName("description")]        public string Description { get; init; } = "";
    [JsonPropertyName("group_type")]         public string GroupType { get; init; } = "";
    [JsonPropertyName("member_count")]       public int MemberCount { get; init; }
}

public sealed record AdComputer
{
    [JsonPropertyName("name")]               public string Name { get; init; } = "";
    [JsonPropertyName("dn")]                 public string Dn { get; init; } = "";
    [JsonPropertyName("dns_hostname")]       public string DnsHostname { get; init; } = "";
    [JsonPropertyName("operating_system")]   public string OperatingSystem { get; init; } = "";
    [JsonPropertyName("enabled")]            public bool Enabled { get; init; }
}
