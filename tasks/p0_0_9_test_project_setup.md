# Task: Test project setup:

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.9 Test Infrastructure**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **Test project setup:**
  - Verify `Mcpw.Tests.csproj` references: xUnit, Moq/NSubstitute, FluentAssertions
  - Create test helpers: `MockWmiClient`, `MockEventLogAccess`, `MockRegistryAccess`, `MockServiceControl`, `MockPowerShellHost`
  - Create `TestFixtures/` folder with JSON fixture data for WMI responses, AD objects, Docker API responses
  - Create `ServerTestHelper` — wraps `McpServer` with `StringReader`/`StringWriter` for easy JSON-RPC round-trip testing
  - File: `tests/Mcpw.Tests/`

## Tool Specifications

