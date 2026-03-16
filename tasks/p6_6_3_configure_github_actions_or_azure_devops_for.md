# Task: Configure GitHub Actions (or Azure DevOps) for:

**Phase 6: Polish & Finalize**
**Sub-phase: 6.3 CI/CD Pipeline**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] Configure GitHub Actions (or Azure DevOps) for:
  - Build: `dotnet build`
  - Unit tests: `dotnet test --filter "Category!=Integration&Category!=Performance"`
  - Publish: `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
  - Nightly: integration + performance tests on Windows runner

## Tool Specifications

### Test Spec: Configuration
### Configuration

- ✅ Tool in `enabledDomains` is accessible
- ✅ Tool in `disabledDomains` returns ToolNotFound
- ✅ Tool not in either list returns ToolNotFound
- ✅ Config file missing → starts with safe defaults
- ✅ Config file malformed JSON → fails with clear error, does not start
- ✅ Config reload does not interrupt in-flight tool calls

