# Task: InputValidator.cs

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.4 Input Validation**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **InputValidator.cs** — Add missing validators: null byte check, Unicode normalization, ADS check, device path check, UNC check, max length check
  - File: `src/Mcpw/InputValidator.cs`

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

### Test Spec: Security (Global)
### Security (Global)

- 🔒 No tool leaks stack traces in error responses
- 🔒 No tool includes raw exception messages from .NET framework
- 🔒 No tool returns data from outside allowed paths
- 🔒 No tool executes when privilege tier is insufficient
- 🔒 All string inputs are sanitized against injection (command, WQL, LDAP, XPath)
- 🔒 Unicode normalization applied before path validation (prevent path bypass via combining characters)
- 🔒 Null bytes in string inputs are rejected
- 🔒 Extremely long input strings (>64KB) are rejected with appropriate error
- 🔒 Concurrent calls to the same tool do not cause race conditions or data corruption

---

