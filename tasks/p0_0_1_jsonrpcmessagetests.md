# Task: JsonRpcMessageTests

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.1 MCP Types & JSON-RPC**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` **JsonRpcMessageTests** — Verify serialization/deserialization of `JsonRpcRequest`, `JsonRpcResponse`, `JsonRpcError` (already started in `tests/Protocol/JsonRpcMessageTests.cs`)
  - ✅ Valid request round-trips
  - ✅ Error response includes code + message, no data leak
  - ✅ Null id handled
  - ✅ Params as object or array

## Tool Specifications

### Test Spec: MCP Protocol Compliance
### MCP Protocol Compliance

- ✅ Returns valid JSON-RPC 2.0 response for every tool call
- ✅ Returns `error` object with `code` and `message` for invalid requests
- ✅ Returns `error` with code `-32601` (Method Not Found) for disabled domains
- ✅ Returns `error` with code `-32602` (Invalid Params) for missing required parameters
- ✅ Returns `error` with code `-32602` for wrong parameter types (string where integer expected, etc.)
- ✅ Returns `error` with code `-32603` (Internal Error) for unexpected failures, with no stack trace leaked
- ✅ Tool name in request matches exactly (case-sensitive)
- ✅ Extra/unknown parameters are ignored (not rejected)
- ✅ Response includes all documented required fields
- ✅ Response field types match documentation (string is string, not integer, etc.)
- ✅ Null/optional fields are present as `null`, not omitted
- ✅ Timestamps are always ISO 8601 UTC format
- ✅ Tool manifest (`tools/list`) includes all enabled tools with correct names, descriptions, and input schemas

