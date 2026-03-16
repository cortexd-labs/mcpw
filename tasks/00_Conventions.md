# Global Reference: Conventions

## Features: Conventions
## Conventions

**Privilege Tiers:**

- 🟢 **Read** — `LOCAL SERVICE` or any user. No system changes.
- 🟡 **Operate** — Local Administrator. Modifies system state.
- 🔵 **Domain** — Domain account with delegated permissions.
- 🔴 **Dangerous** — Requires explicit neurond policy approval. Potentially destructive.

**Input parameters** use JSON Schema. All parameters are required unless marked `(optional)`.

**Response fields** document the JSON object returned in the MCP `tools/call` result.

---

