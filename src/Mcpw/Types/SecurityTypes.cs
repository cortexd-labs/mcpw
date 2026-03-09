using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record CertificateInfo
{
    [JsonPropertyName("subject")]      public string Subject { get; init; } = "";
    [JsonPropertyName("issuer")]       public string Issuer { get; init; } = "";
    [JsonPropertyName("thumbprint")]   public string Thumbprint { get; init; } = "";
    [JsonPropertyName("not_before")]   public string NotBefore { get; init; } = "";
    [JsonPropertyName("not_after")]    public string NotAfter { get; init; } = "";
    [JsonPropertyName("store")]        public string Store { get; init; } = "";
    [JsonPropertyName("has_private_key")]public bool HasPrivateKey { get; init; }
}
