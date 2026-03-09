using System.Text.Json;
using Mcpw.Types;

namespace Mcpw.Tests.Protocol;

public sealed class JsonRpcMessageTests
{
    private static readonly JsonSerializerOptions Opts = McpJson.Options;

    // ── Serialization round-trips ──────────────────────────────────────────

    [Fact]
    public void Request_roundtrip_preserves_all_fields()
    {
        var json = """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":null}""";
        var req  = JsonSerializer.Deserialize<JsonRpcRequest>(json, Opts)!;

        req.Jsonrpc.Should().Be("2.0");
        req.Method.Should().Be("tools/list");
        JsonSerializer.Serialize(req, Opts).Should().Contain("tools/list");
    }

    [Fact]
    public void Response_with_result_serializes_without_error_field()
    {
        var response = new JsonRpcResponse
        {
            Id     = JsonSerializer.SerializeToElement(1),
            Result = JsonSerializer.SerializeToElement(new { ok = true }),
        };
        var json = JsonSerializer.Serialize(response, Opts);
        json.Should().NotContain("\"error\"");
        json.Should().Contain("\"result\"");
    }

    [Fact]
    public void Response_with_error_serializes_without_result_field()
    {
        var response = new JsonRpcResponse
        {
            Id    = JsonSerializer.SerializeToElement(2),
            Error = new JsonRpcError { Code = -32601, Message = "Method not found" },
        };
        var json = JsonSerializer.Serialize(response, Opts);
        json.Should().NotContain("\"result\"");
        json.Should().Contain("\"error\"");
        json.Should().Contain("-32601");
    }

    // ── MCP initialize result ─────────────────────────────────────────────

    [Fact]
    public void McpInitializeResult_serializes_expected_shape()
    {
        var result = new McpInitializeResult();
        var json   = JsonSerializer.Serialize(result, Opts);

        json.Should().Contain("2024-11-05");
        json.Should().Contain("mcpw");
        json.Should().Contain("tools");
    }

    // ── McpJson helpers ───────────────────────────────────────────────────

    [Fact]
    public void McpJson_ErrorResult_sets_isError_true()
    {
        var r = McpJson.ErrorResult("something went wrong");
        r.IsError.Should().BeTrue();
        r.Content.Should().HaveCount(1);
        r.Content[0].Text.Should().Contain("something went wrong");
    }

    [Fact]
    public void McpJson_TextResult_sets_isError_false()
    {
        var r = McpJson.TextResult("hello");
        r.IsError.Should().BeFalse();
        r.Content[0].Text.Should().Be("hello");
    }

    [Fact]
    public void McpJson_JsonResult_serializes_object_to_text()
    {
        var r = McpJson.JsonResult(new { name = "test", value = 42 });
        r.IsError.Should().BeFalse();
        r.Content[0].Text.Should().Contain("\"name\"");
        r.Content[0].Text.Should().Contain("42");
    }

    // ── Error codes ───────────────────────────────────────────────────────

    [Fact]
    public void RpcErrorCodes_have_expected_values()
    {
        RpcErrorCodes.ParseError.Should().Be(-32700);
        RpcErrorCodes.MethodNotFound.Should().Be(-32601);
        RpcErrorCodes.InvalidParams.Should().Be(-32602);
    }
}
