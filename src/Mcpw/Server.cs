using System.Text.Json;
using Mcpw.Types;

namespace Mcpw;

/// <summary>
/// MCP JSON-RPC server that reads requests from a TextReader and writes
/// responses to a TextWriter. Transport-agnostic: stdio in production,
/// StringReader/StringWriter in tests.
/// </summary>
public sealed class McpServer
{
    private readonly IReadOnlyDictionary<string, IToolHandler> _handlers;
    private readonly McpwConfig _config;

    public McpServer(IEnumerable<IToolHandler> handlers, McpwConfig config)
    {
        _config = config;
        _handlers = handlers
            .Where(h => config.IsDomainEnabled(h.Domain))
            .ToDictionary(h => h.Domain, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Process a single newline-delimited JSON-RPC request line.</summary>
    public async Task<string> HandleLineAsync(string line, CancellationToken ct = default)
    {
        JsonRpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<JsonRpcRequest>(line, McpJson.Options);
            if (request is null) return ErrorResponse(null, RpcErrorCodes.ParseError, "Null request");
        }
        catch (JsonException ex)
        {
            return ErrorResponse(null, RpcErrorCodes.ParseError, ex.Message);
        }

        try
        {
            return request.Method switch
            {
                "initialize"     => await HandleInitialize(request, ct),
                "tools/list"     => await HandleToolsList(request, ct),
                "tools/call"     => await HandleToolsCall(request, ct),
                "notifications/initialized" => "", // fire-and-forget, no response
                _                => ErrorResponse(request.Id, RpcErrorCodes.MethodNotFound,
                                        $"Method not found: {request.Method}"),
            };
        }
        catch (Exception ex)
        {
            return ErrorResponse(request.Id, RpcErrorCodes.InternalError, ex.Message);
        }
    }

    /// <summary>Run the stdio loop until the reader is exhausted or cancellation fires.</summary>
    public async Task RunAsync(TextReader reader, TextWriter writer, CancellationToken ct = default)
    {
        string? line;
        while (!ct.IsCancellationRequested && (line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var response = await HandleLineAsync(line, ct);
            if (!string.IsNullOrEmpty(response))
            {
                await writer.WriteLineAsync(response.AsMemory(), ct);
                await writer.FlushAsync(ct);
            }
        }
    }

    // ── method handlers ────────────────────────────────────────────────────

    private Task<string> HandleInitialize(JsonRpcRequest req, CancellationToken _)
    {
        var result = new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities    = new McpCapabilities { Tools = new McpToolsCapability() },
            ServerInfo      = new McpServerInfo { Name = "mcpw", Version = "0.1.0" },
        };
        return Task.FromResult(SuccessResponse(req.Id, result));
    }

    private Task<string> HandleToolsList(JsonRpcRequest req, CancellationToken _)
    {
        var tools = _handlers.Values
            .SelectMany(h => h.GetTools())
            .ToList();

        return Task.FromResult(SuccessResponse(req.Id, new McpToolsListResult { Tools = tools }));
    }

    private async Task<string> HandleToolsCall(JsonRpcRequest req, CancellationToken ct)
    {
        McpCallToolParams? p;
        try
        {
            p = req.Params.HasValue
                ? JsonSerializer.Deserialize<McpCallToolParams>(req.Params.Value, McpJson.Options)
                : null;
        }
        catch (JsonException ex)
        {
            return ErrorResponse(req.Id, RpcErrorCodes.InvalidParams, ex.Message);
        }

        if (p is null || string.IsNullOrEmpty(p.Name))
            return ErrorResponse(req.Id, RpcErrorCodes.InvalidParams, "Missing tool name");

        var domain = p.Name.Contains('.') ? p.Name[..p.Name.IndexOf('.')] : p.Name;

        if (!_handlers.TryGetValue(domain, out var handler))
            return ErrorResponse(req.Id, RpcErrorCodes.MethodNotFound, $"Tool not found: {p.Name}");

        McpCallToolResult toolResult;
        try
        {
            toolResult = await handler.CallAsync(p.Name, p.Arguments, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            toolResult = McpJson.ErrorResult($"Access denied: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            toolResult = McpJson.ErrorResult($"Invalid argument: {ex.Message}");
        }
        catch (Exception ex)
        {
            toolResult = McpJson.ErrorResult(ex.Message);
        }

        return SuccessResponse(req.Id, toolResult);
    }

    // ── JSON-RPC helpers ──────────────────────────────────────────────────

    private static string SuccessResponse<T>(JsonElement? id, T result)
    {
        var response = new JsonRpcResponse
        {
            Id     = id,
            Result = JsonSerializer.SerializeToElement(result, McpJson.Options),
        };
        return JsonSerializer.Serialize(response, McpJson.Options);
    }

    private static string ErrorResponse(JsonElement? id, int code, string message)
    {
        var response = new JsonRpcResponse
        {
            Id    = id,
            Error = new JsonRpcError { Code = code, Message = message },
        };
        return JsonSerializer.Serialize(response, McpJson.Options);
    }
}
