using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record PciDevice
{
    [JsonPropertyName("device_id")]    public string DeviceId { get; init; } = "";
    [JsonPropertyName("name")]         public string Name { get; init; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; init; } = "";
    [JsonPropertyName("class")]        public string Class { get; init; } = "";
    [JsonPropertyName("status")]       public string Status { get; init; } = "";
}

public sealed record UsbDevice
{
    [JsonPropertyName("device_id")]    public string DeviceId { get; init; } = "";
    [JsonPropertyName("name")]         public string Name { get; init; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; init; } = "";
    [JsonPropertyName("status")]       public string Status { get; init; } = "";
}
