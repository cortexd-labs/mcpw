# Task: ServerTests

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.2 Server (JSON-RPC Router)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` **ServerTests** — (already started in `tests/ServerTests.cs`)
  - ✅ `initialize` returns protocol version and capabilities
  - ✅ `tools/list` returns enabled tools only
  - ✅ `tools/call` routes to correct handler domain
  - ✅ Unknown method → -32601 MethodNotFound
  - ✅ Missing tool name → -32602 InvalidParams
  - ✅ Tool in disabled domain → -32601
  - ✅ Malformed JSON → -32700 ParseError
  - ✅ Handler throws `ArgumentException` → error result (not crash)
  - ✅ Handler throws `UnauthorizedAccessException` → access denied result
  - ✅ Handler throws unexpected exception → -32603 with safe message
  - ✅ `notifications/initialized` → no response (empty string)
  - ✅ Extra params ignored
  - ✅ Concurrent calls do not race

## Tool Specifications

### Feature: Conventions
## Conventions

**Privilege Tiers:**

- 🟢 **Read** — `LOCAL SERVICE` or any user. No system changes.
- 🟡 **Operate** — Local Administrator. Modifies system state.
- 🔵 **Domain** — Domain account with delegated permissions.
- 🔴 **Dangerous** — Requires explicit neurond policy approval. Potentially destructive.

**Input parameters** use JSON Schema. All parameters are required unless marked `(optional)`.

**Response fields** document the JSON object returned in the MCP `tools/call` result.

---

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

