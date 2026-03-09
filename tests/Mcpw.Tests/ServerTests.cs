using System.Text.Json;
using Mcpw;
using Mcpw.Types;

namespace Mcpw.Tests;

public sealed class ServerTests
{
    private static McpServer BuildServer(params IToolHandler[] handlers)
    {
        var config = new McpwConfig(); // all domains enabled by default
        return new McpServer(handlers, config);
    }

    // ── initialize ────────────────────────────────────────────────────────

    [Fact]
    public async Task Initialize_returns_protocol_version_and_server_info()
    {
        var server = BuildServer();
        var req    = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}""";

        var response = await server.HandleLineAsync(req);

        response.Should().Contain("2024-11-05");
        response.Should().Contain("mcpw");
        response.Should().Contain("\"id\":1");
    }

    // ── tools/list ────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolsList_returns_all_tools_from_registered_handlers()
    {
        var handler = new FakeHandler("demo", ["demo.ping", "demo.pong"]);
        var server  = BuildServer(handler);

        var response = await server.HandleLineAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        response.Should().Contain("demo.ping");
        response.Should().Contain("demo.pong");
    }

    [Fact]
    public async Task ToolsList_returns_empty_tools_when_no_handlers_registered()
    {
        var server   = BuildServer();
        var response = await server.HandleLineAsync("""{"jsonrpc":"2.0","id":3,"method":"tools/list"}""");

        response.Should().Contain("\"tools\"");
        response.Should().Contain("[]");
    }

    // ── tools/call ────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_dispatches_to_correct_handler()
    {
        var handler = new FakeHandler("demo", ["demo.ping"]);
        var server  = BuildServer(handler);

        var response = await server.HandleLineAsync(
            """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"demo.ping","arguments":{}}}""");

        response.Should().Contain("pong");
        handler.LastTool.Should().Be("demo.ping");
    }

    [Fact]
    public async Task ToolsCall_returns_MethodNotFound_for_unknown_domain()
    {
        var server   = BuildServer();
        var response = await server.HandleLineAsync(
            """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"ghost.tool"}}""");

        response.Should().Contain("-32601");
        response.Should().Contain("not found");
    }

    // ── disabled domains ──────────────────────────────────────────────────

    [Fact]
    public async Task ToolsList_excludes_disabled_domain_handlers()
    {
        var config  = new McpwConfig
        {
            EnabledDomains  = ["demo"],
            DisabledDomains = ["secret"],
        };
        var visible = new FakeHandler("demo",   ["demo.a"]);
        var hidden  = new FakeHandler("secret", ["secret.b"]);
        var server  = new McpServer([visible, hidden], config);

        var response = await server.HandleLineAsync("""{"jsonrpc":"2.0","id":6,"method":"tools/list"}""");

        response.Should().Contain("demo.a");
        response.Should().NotContain("secret.b");
    }

    // ── error handling ────────────────────────────────────────────────────

    [Fact]
    public async Task HandleLine_returns_ParseError_for_malformed_json()
    {
        var server   = BuildServer();
        var response = await server.HandleLineAsync("not json at all {{{");

        response.Should().Contain("-32700");
    }

    [Fact]
    public async Task HandleLine_returns_MethodNotFound_for_unknown_method()
    {
        var server   = BuildServer();
        var response = await server.HandleLineAsync("""{"jsonrpc":"2.0","id":7,"method":"foo/bar"}""");

        response.Should().Contain("-32601");
    }

    [Fact]
    public async Task HandleLine_returns_empty_string_for_notifications()
    {
        var server   = BuildServer();
        var response = await server.HandleLineAsync(
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""");

        response.Should().BeEmpty();
    }

    // ── RunAsync stdio loop ───────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_processes_multiple_lines()
    {
        var server  = BuildServer();
        var input   = new StringReader(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}""" + "\n" +
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""" + "\n");
        var output  = new StringWriter();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await server.RunAsync(input, output, cts.Token);

        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().Contain("2024-11-05");
        lines[1].Should().Contain("\"tools\"");
    }
}

// ── Test double ──────────────────────────────────────────────────────────────

internal sealed class FakeHandler : IToolHandler
{
    private readonly string[] _tools;
    public string? LastTool { get; private set; }

    public FakeHandler(string domain, string[] tools)
    {
        Domain  = domain;
        _tools  = tools;
    }

    public string Domain { get; }

    public IEnumerable<McpToolDefinition> GetTools() =>
        _tools.Select(t => new McpToolDefinition
        {
            Name        = t,
            Description = $"Fake {t}",
            InputSchema = JsonSerializer.Deserialize<JsonElement>("{}"),
        });

    public Task<McpCallToolResult> CallAsync(string toolName, JsonElement? args, CancellationToken ct = default)
    {
        LastTool = toolName;
        return Task.FromResult(McpJson.TextResult("pong"));
    }
}
