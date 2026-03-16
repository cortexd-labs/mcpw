# Task: Implement all concrete classes:

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.6 Windows Abstraction Implementations**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **Implement all concrete classes:**
  - `WmiClient` — `System.Management` ManagementObjectSearcher
  - `EventLogAccess` — `System.Diagnostics.Eventing.Reader`
  - `RegistryAccess` — `Microsoft.Win32.Registry`
  - `ServiceControl` — `System.ServiceProcess.ServiceController`
  - `PowerShellHost` — `System.Management.Automation` (Constrained Language Mode)
  - Files: `src/Mcpw/Windows/*.cs`

## Tool Specifications

