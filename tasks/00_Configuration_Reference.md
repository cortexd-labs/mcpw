# Global Reference: Configuration_Reference

## Tests: Configuration
### Configuration

- ✅ Tool in `enabledDomains` is accessible
- ✅ Tool in `disabledDomains` returns ToolNotFound
- ✅ Tool not in either list returns ToolNotFound
- ✅ Config file missing → starts with safe defaults
- ✅ Config file malformed JSON → fails with clear error, does not start
- ✅ Config reload does not interrupt in-flight tool calls

