# Task: `[I]` Create `mcpw-test-svc` — Minimal Windows Service for testing:

**Phase 5: Cross-Cutting Concerns & Integration**
**Sub-phase: 5.4 Test Service for service.\* Integration Tests**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` Create `mcpw-test-svc` — Minimal Windows Service for testing:
  - Installs via `sc.exe create mcpw-test-svc`
  - Supports start/stop/pause
  - Logs to known Event Log source
  - Configurable startup behavior (fast, slow, crash-on-start)
  - Auto-cleanup in test teardown

