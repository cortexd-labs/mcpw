# Task: `[T]` Create `tests/PrivilegeTierTests.cs`

**Phase 3: Mutating/Operate-Tier Tools**
**Sub-phase: 3.3 Privilege Tier Enforcement**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` Create `tests/PrivilegeTierTests.cs`
  - ✅ Read tool accessible at all tiers
  - ✅ Operate tool blocked below Operate tier
  - ✅ Domain tool blocked below Domain tier
  - ✅ Dangerous tool blocked below Dangerous tier
  - ✅ Tier from config correctly enforced
  - 🔒 Insufficient tier → clear error, not crash

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

