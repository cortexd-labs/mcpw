# Task: Finalize all interfaces:

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.5 Windows Abstraction Interfaces**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **Finalize all interfaces:**
  - `IWmiClient` — `QueryAsync(string wqlQuery)`, `QueryAsync<T>(...)`, typed methods per WMI class
  - `IEventLogAccess` — `ReadAsync(channel, query, count)`, `GetChannels()`, `ClearAsync()`, `Watch()`
  - `IRegistryAccess` — `GetValue()`, `SetValue()`, `DeleteValue()`, `DeleteKey()`, `GetSubKeyNames()`, `GetValueNames()`, `SearchAsync()`
  - `IServiceControl` — `GetServices()`, `GetStatus()`, `Start()`, `Stop()`, `SetStartupType()`, `GetConfig()`
  - `IPowerShellHost` — `InvokeAsync(script, params)` with Constrained Language Mode
  - Files: `src/Mcpw/Windows/I*.cs`

## Tool Specifications

