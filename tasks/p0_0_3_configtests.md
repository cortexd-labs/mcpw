# Task: ConfigTests

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.3 Configuration**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` **ConfigTests** — (already started in `tests/ConfigTests.cs`)
  - ✅ Missing config file → safe defaults
  - ✅ Valid config → all properties loaded
  - ✅ `enabledDomains` controls `IsDomainEnabled()`
  - ✅ `disabledDomains` overrides `enabledDomains`
  - ✅ Domain not in either list → disabled
  - ✅ Malformed JSON → throws clear error
  - ✅ Empty `enabledDomains` → all enabled
  - ✅ Case-insensitive domain matching
  - 🔒 Config path not injectable (hardcoded default)

## Tool Specifications

### Test Spec: Configuration
### Configuration

- ✅ Tool in `enabledDomains` is accessible
- ✅ Tool in `disabledDomains` returns ToolNotFound
- ✅ Tool not in either list returns ToolNotFound
- ✅ Config file missing → starts with safe defaults
- ✅ Config file malformed JSON → fails with clear error, does not start
- ✅ Config reload does not interrupt in-flight tool calls

