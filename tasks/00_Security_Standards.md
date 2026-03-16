# Global Reference: Security_Standards

## Tests: Security (Global)
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

