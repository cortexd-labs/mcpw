using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using Mcpw.Types;

namespace Mcpw.Tools;

/// <summary>
/// Communicates with Docker Engine via its named pipe on Windows:
/// \\.\pipe\docker_engine  →  Unix domain socket equivalent on Windows.
/// </summary>
public sealed class ContainerTools : IToolHandler
{
    private const string DockerPipe = @"\\.\pipe\docker_engine";

    public string Domain => "container";

    public IEnumerable<McpToolDefinition> GetTools() =>
    [
        Tool("container.list",    "List Docker containers",                           PrivilegeTier.Read,  "{}"),
        Tool("container.inspect", "Detailed container info",                          PrivilegeTier.Read,
            """{"type":"object","required":["id"],"properties":{"id":{"type":"string"}}}"""),
        Tool("container.logs",    "Container stdout/stderr logs",                     PrivilegeTier.Read,
            """{"type":"object","required":["id"],"properties":{"id":{"type":"string"},"tail":{"type":"integer","default":100}}}"""),
        Tool("container.start",   "Start a stopped container",                        PrivilegeTier.Operate,
            """{"type":"object","required":["id"],"properties":{"id":{"type":"string"}}}"""),
        Tool("container.stop",    "Stop a running container",                         PrivilegeTier.Operate,
            """{"type":"object","required":["id"],"properties":{"id":{"type":"string"},"timeout":{"type":"integer","default":10}}}"""),
    ];

    public async Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        using var client = CreateDockerClient();
        try
        {
            return toolName switch
            {
                "container.list"    => await ListContainers(client, ct),
                "container.inspect" => await InspectContainer(client, args, ct),
                "container.logs"    => await ContainerLogs(client, args, ct),
                "container.start"   => await ContainerOp(client, args, "start", ct),
                "container.stop"    => await ContainerOp(client, args, "stop",  ct),
                _                   => McpJson.ErrorResult($"Unknown tool: {toolName}"),
            };
        }
        catch (Exception ex)
        {
            return McpJson.ErrorResult($"Docker unavailable: {ex.Message}");
        }
    }

    private static HttpClient CreateDockerClient()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (ctx, ct) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(DockerPipe), ct);
                return new NetworkStream(socket, ownsSocket: true);
            },
        };
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
    }

    private static async Task<McpCallToolResult> ListContainers(HttpClient client, CancellationToken ct)
    {
        var json = await client.GetStringAsync("/v1.43/containers/json?all=true", ct);
        return McpJson.TextResult(json);
    }

    private static async Task<McpCallToolResult> InspectContainer(HttpClient client, JsonElement? args, CancellationToken ct)
    {
        var id = RequiredString(args, "id");
        if (id is null) return McpJson.ErrorResult("Missing required argument: id");
        InputValidator.AssertNoInjection(id, "id");
        var json = await client.GetStringAsync($"/v1.43/containers/{id}/json", ct);
        return McpJson.TextResult(json);
    }

    private static async Task<McpCallToolResult> ContainerLogs(HttpClient client, JsonElement? args, CancellationToken ct)
    {
        var id   = RequiredString(args, "id");
        if (id is null) return McpJson.ErrorResult("Missing required argument: id");
        var tail = args?.TryGetProperty("tail", out var t) == true ? t.GetInt32() : 100;
        InputValidator.AssertNoInjection(id, "id");
        var logs = await client.GetStringAsync($"/v1.43/containers/{id}/logs?stdout=true&stderr=true&tail={tail}", ct);
        return McpJson.TextResult(logs);
    }

    private static async Task<McpCallToolResult> ContainerOp(HttpClient client, JsonElement? args, string op, CancellationToken ct)
    {
        var id = RequiredString(args, "id");
        if (id is null) return McpJson.ErrorResult("Missing required argument: id");
        InputValidator.AssertNoInjection(id, "id");
        var suffix = op == "stop" && args?.TryGetProperty("timeout", out var to) == true ? $"?t={to.GetInt32()}" : "";
        using var response = await client.PostAsync($"/v1.43/containers/{id}/{op}{suffix}", null, ct);
        return McpJson.TextResult($"Container {id}: {op} → HTTP {(int)response.StatusCode}");
    }

    private static string? RequiredString(JsonElement? args, string key) =>
        args?.TryGetProperty(key, out var v) == true ? v.GetString() : null;

    private static McpToolDefinition Tool(string name, string desc, PrivilegeTier tier, string schema) =>
        new() { Name = name, Description = desc, Tier = tier,
                InputSchema = JsonSerializer.Deserialize<JsonElement>(schema) };
}
