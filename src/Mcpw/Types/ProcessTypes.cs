using System.Text.Json.Serialization;

namespace Mcpw.Types;

public sealed record ProcessInfo
{
    [JsonPropertyName("pid")]              public int Pid { get; init; }
    [JsonPropertyName("name")]             public string Name { get; init; } = "";
    [JsonPropertyName("status")]           public string Status { get; init; } = "";
    [JsonPropertyName("cpu_percent")]      public double CpuPercent { get; init; }
    [JsonPropertyName("memory_bytes")]     public long MemoryBytes { get; init; }
    [JsonPropertyName("thread_count")]     public int ThreadCount { get; init; }
    [JsonPropertyName("parent_pid")]       public int? ParentPid { get; init; }
    [JsonPropertyName("start_time")]       public string StartTime { get; init; } = "";
    [JsonPropertyName("executable_path")]  public string ExecutablePath { get; init; } = "";
}

public sealed record ProcessTreeNode
{
    [JsonPropertyName("pid")]      public int Pid { get; init; }
    [JsonPropertyName("name")]     public string Name { get; init; } = "";
    [JsonPropertyName("children")] public IReadOnlyList<ProcessTreeNode> Children { get; init; } = [];
}
