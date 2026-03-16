# Task: `[T]` Create `tests/GlobalTests.cs` — Tests from specs/tests.md "Global Tests" section applied to AL

**Phase 5: Cross-Cutting Concerns & Integration**
**Sub-phase: 5.1 Global Test Suite**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` Create `tests/GlobalTests.cs` — Tests from specs/tests.md "Global Tests" section applied to ALL tools:
  - ✅ Valid JSON-RPC 2.0 response for every tool call (parameterized test over all enabled tools)
  - ✅ Error code -32601 for disabled domains
  - ✅ Error code -32602 for missing required params
  - ✅ Error code -32602 for wrong param types
  - ✅ Error code -32603 for unexpected failures, no stack trace
  - ✅ Tool name case-sensitive matching
  - ✅ Extra params ignored
  - ✅ Response includes all documented required fields
  - ✅ Response field types match documentation
  - ✅ Null/optional present as null, not omitted
  - ✅ Timestamps ISO 8601 UTC
  - ✅ Tool manifest includes all enabled tools with correct schemas
  - 🔒 No stack traces in errors
  - 🔒 No raw .NET exception messages
  - 🔒 No data outside allowed paths
  - 🔒 Privilege tier enforced
  - 🔒 Injection chars rejected in all string inputs
  - 🔒 Unicode normalization before path validation
  - 🔒 Null bytes rejected
  - 🔒 Strings > 64KB rejected
  - 🔒 Concurrent calls no race conditions

