# Task: Config.cs / ConfigLoader

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.3 Configuration**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **Config.cs / ConfigLoader** — Finalize
  - File: `src/Mcpw/Config.cs`

## Tool Specifications

### Test Spec: Configuration
### Configuration

- ✅ Tool in `enabledDomains` is accessible
- ✅ Tool in `disabledDomains` returns ToolNotFound
- ✅ Tool not in either list returns ToolNotFound
- ✅ Config file missing → starts with safe defaults
- ✅ Config file malformed JSON → fails with clear error, does not start
- ✅ Config reload does not interrupt in-flight tool calls

