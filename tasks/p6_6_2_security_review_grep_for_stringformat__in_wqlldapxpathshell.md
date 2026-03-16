# Task: Security review: grep for `string.Format`, `$"..."` in WQL/LDAP/XPath/shell contexts

**Phase 6: Polish & Finalize**
**Sub-phase: 6.2 Final Review**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] Security review: grep for `string.Format`, `$"..."` in WQL/LDAP/XPath/shell contexts

## Tool Specifications

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

