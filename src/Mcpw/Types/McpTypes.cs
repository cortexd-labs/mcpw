using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcpw.Types;

// ── JSON-RPC 2.0 envelope ──────────────────────────────────────────────────

public sealed record JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string Jsonrpc { get; init; } = "2.0";
    [JsonPropertyName("id")]      public JsonElement? Id { get; init; }
    [JsonPropertyName("method")]  public string Method { get; init; } = "";
    [JsonPropertyName("params")]  public JsonElement? Params { get; init; }
}

public sealed record JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")] public string Jsonrpc { get; init; } = "2.0";
    [JsonPropertyName("id")]      public JsonElement? Id { get; init; }
    [JsonPropertyName("result")]  public JsonElement? Result { get; init; }
    [JsonPropertyName("error")]   public JsonRpcError? Error { get; init; }
}

public sealed record JsonRpcError
{
    [JsonPropertyName("code")]    public int Code { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = "";
    [JsonPropertyName("data")]    public JsonElement? Data { get; init; }
}

// Standard JSON-RPC error codes
public static class RpcErrorCodes
{
    public const int ParseError     = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams  = -32602;
    public const int InternalError  = -32603;
}

// ── MCP protocol types ─────────────────────────────────────────────────────

public sealed record McpServerInfo
{
    [JsonPropertyName("name")]    public string Name { get; init; } = "mcpw";
    [JsonPropertyName("version")] public string Version { get; init; } = "0.1.0";
}

public sealed record McpCapabilities
{
    [JsonPropertyName("tools")] public McpToolsCapability? Tools { get; init; }
}

public sealed record McpToolsCapability
{
    [JsonPropertyName("listChanged")] public bool ListChanged { get; init; } = false;
}

public sealed record McpInitializeResult
{
    [JsonPropertyName("protocolVersion")] public string ProtocolVersion { get; init; } = "2024-11-05";
    [JsonPropertyName("capabilities")]    public McpCapabilities Capabilities { get; init; } = new();
    [JsonPropertyName("serverInfo")]      public McpServerInfo ServerInfo { get; init; } = new();
}

// ── Tool definitions ───────────────────────────────────────────────────────

public enum PrivilegeTier { Read, Operate, Domain, Dangerous }

public sealed record McpToolDefinition
{
    [JsonPropertyName("name")]        public string Name { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("inputSchema")] public JsonElement InputSchema { get; init; }

    [JsonIgnore] public PrivilegeTier Tier { get; init; } = PrivilegeTier.Read;
}

public sealed record McpToolsListResult
{
    [JsonPropertyName("tools")] public IReadOnlyList<McpToolDefinition> Tools { get; init; } = [];
}

// ── Tool call / response ───────────────────────────────────────────────────

public sealed record McpCallToolParams
{
    [JsonPropertyName("name")]      public string Name { get; init; } = "";
    [JsonPropertyName("arguments")] public JsonElement? Arguments { get; init; }
}

public sealed record McpContent
{
    [JsonPropertyName("type")] public string Type { get; init; } = "text";
    [JsonPropertyName("text")] public string Text { get; init; } = "";
}

public sealed record McpCallToolResult
{
    [JsonPropertyName("content")]   public IReadOnlyList<McpContent> Content { get; init; } = [];
    [JsonPropertyName("isError")]   public bool IsError { get; init; } = false;
}

// ── Shared JSON options ────────────────────────────────────────────────────

public static class McpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };

    public static McpCallToolResult ErrorResult(string message) => new()
    {
        IsError = true,
        Content = [new McpContent { Type = "text", Text = message }],
    };

    public static McpCallToolResult TextResult(string text) => new()
    {
        Content = [new McpContent { Type = "text", Text = text }],
    };

    public static McpCallToolResult JsonResult<T>(T value) => new()
    {
        Content = [new McpContent
        {
            Type = "text",
            Text = JsonSerializer.Serialize(value, Options),
        }],
    };
}
