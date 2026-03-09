using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record DiskInfo
{
    [JsonPropertyName("device_id")]    public string DeviceId { get; init; } = "";
    [JsonPropertyName("model")]        public string Model { get; init; } = "";
    [JsonPropertyName("size_bytes")]   public long SizeBytes { get; init; }
    [JsonPropertyName("partitions")]   public int Partitions { get; init; }
    [JsonPropertyName("interface")]    public string Interface { get; init; } = "";
    [JsonPropertyName("serial")]       public string Serial { get; init; } = "";
}

public sealed record VolumeInfo
{
    [JsonPropertyName("name")]          public string Name { get; init; } = "";
    [JsonPropertyName("label")]         public string Label { get; init; } = "";
    [JsonPropertyName("drive_letter")]  public string DriveLetter { get; init; } = "";
    [JsonPropertyName("file_system")]   public string FileSystem { get; init; } = "";
    [JsonPropertyName("capacity_bytes")]public long CapacityBytes { get; init; }
    [JsonPropertyName("free_bytes")]    public long FreeBytes { get; init; }
}
