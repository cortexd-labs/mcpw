# Task: InputValidatorTests

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.4 Input Validation**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` **InputValidatorTests** — (already started in `tests/InputValidatorTests.cs`)
  - ✅ `AssertNoInjection` rejects each injection char
  - ✅ `AssertNoInjection` accepts clean strings
  - ✅ `SanitizePath` blocks `..` traversal
  - ✅ `SanitizePath` blocks `C:\Windows\System32\config`
  - ✅ `SanitizePath` blocks `C:\Windows\NTDS`
  - ✅ `SanitizePath` canonicalizes and returns full path
  - ✅ `SanitizePath(path, allowlist)` blocks paths outside allowlist
  - ✅ `SanitizePath(path, allowlist)` allows paths within allowlist
  - 🔒 Null bytes rejected
  - 🔒 Unicode normalization before comparison
  - 🔒 Trailing dots/spaces stripped (Windows auto-strips)
  - 🔒 ADS (`:hidden`) in paths rejected
  - 🔒 Device paths (`\\.\`) rejected
  - 🔒 UNC paths rejected by default
  - 🔒 Strings > 64KB rejected
  - ✅ `ParsePid` valid integer → returns pid
  - ✅ `ParsePid` non-integer → throws
  - ✅ `ParsePid` negative → throws

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

