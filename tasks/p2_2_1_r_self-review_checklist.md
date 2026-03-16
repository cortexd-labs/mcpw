# Task: `[R]` Self-review checklist

**Phase 2: Windows-Specific Read-Only Domains**
**Sub-phase: 2.1 `registry.*` (6 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` Self-review checklist

## Tool Specifications

### Feature: registry.*
## 14. `registry.*` — Windows Registry

### Test Spec: registry.*
## 14. `registry.*`

### Feature: registry.* — Windows Registry
## 14. `registry.*` — Windows Registry

### Feature: registry.delete
### `registry.delete` 🔴 Dangerous

Delete a registry value or key.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `key` | string | Full key path |
| `value` | string (optional) | Value name. If omitted, deletes the entire key. |
| `recursive` | boolean (optional) | For key deletion, delete all subkeys. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `key` | string | Key path |
| `value_name` | string / null | Value deleted (null if key deletion) |
| `deleted` | boolean | Success |
| `subkeys_deleted` | integer | Subkeys deleted (if recursive) |

**Implementation:** `RegistryKey.DeleteValue()` / `RegistryKey.DeleteSubKeyTree()`

---

### Test Spec: registry.delete
### `registry.delete`

**Happy Path:**

- 🎭 Deletes value → `registry.get` returns not found
- 🎭 Deletes key with `recursive: true` → removes all subkeys
- 🎭 Returns count of subkeys deleted

**Input Validation:**

- ✅ Deleting key without `recursive` when subkeys exist → error

**Security:**

- 🔒 Requires Dangerous tier
- 🔒 Cannot delete root hive keys (HKLM, HKCU)
- 🔒 Cannot delete `HKLM\SYSTEM\CurrentControlSet`
- 🔒 All deletions logged

**Error Handling:**

- ✅ Key/value doesn't exist → error
- ✅ Access denied → error

---

### Feature: registry.export
### `registry.export` 🟢 Read

Export a registry key tree in .reg format.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `key` | string | Key path to export |

**Response:**
| Field | Type | Description |
|---|---|---|
| `key` | string | Key exported |
| `content` | string | .reg file content |
| `keys_exported` | integer | Number of keys |
| `values_exported` | integer | Number of values |

**Implementation:** `reg export` or manual serialization to .reg format

---

### Test Spec: registry.export
### `registry.export`

**Happy Path:**

- ✅ Returns valid .reg format content
- ✅ Exported content re-importable via `reg import`
- ✅ Includes all subkeys and values recursively
- ✅ `keys_exported` and `values_exported` counts are correct

**Security:**

- 🔒 Cannot export blocked keys

---

### Feature: registry.get
### `registry.get` 🟢 Read

Read a registry value.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `key` | string | Full key path (e.g., "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion") |
| `value` | string (optional) | Value name. If omitted, returns default value. |

**Response:**
| Field | Type | Description |
|---|---|---|
| `key` | string | Key path |
| `value_name` | string | Value name ("(Default)" if default) |
| `data` | string / integer / array | Value data |
| `type` | string | "REG_SZ" / "REG_DWORD" / "REG_QWORD" / "REG_BINARY" / "REG_MULTI_SZ" / "REG_EXPAND_SZ" |

**Implementation:** `Registry.GetValue()` or `RegistryKey.OpenSubKey().GetValue()`

---

### Test Spec: registry.get
### `registry.get`

**Happy Path:**

- ✅ Reads REG_SZ value correctly
- ✅ Reads REG_DWORD value as integer
- ✅ Reads REG_QWORD value as integer
- ✅ Reads REG_MULTI_SZ as string array
- ✅ Reads REG_EXPAND_SZ with unexpanded variables
- ✅ Reads REG_BINARY as hex string or base64
- ✅ Reads default value when `value` omitted
- ✅ Known key: `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` → returns ProductName
- ✅ `type` field correctly identifies value type

**Input Validation:**

- ✅ Empty `key` → error
- ✅ Invalid root key (not HKLM, HKCU, etc.) → error
- ✅ Malformed key path → error

**Error Handling:**

- ✅ Key doesn't exist → error "Key not found"
- ✅ Value doesn't exist → error "Value not found"
- ✅ Access denied → error

**Security:**

- 🔒 Blocked keys: `HKLM\SAM`, `HKLM\SECURITY` → error
- 🔒 Cannot read `HKLM\SYSTEM\CurrentControlSet\Control\Lsa\Secrets` → error
- 🔒 Key path traversal (e.g., `..` in path) → blocked

**Edge Cases:**

- ⚡ Very large REG_BINARY value (>1MB)
- ⚡ REG_MULTI_SZ with empty strings in array
- ⚡ Key with default value that's empty
- ⚡ Key with no values
- ⚡ HKCU requires user context → works if running as user, fails as SYSTEM

---

### Feature: registry.list
### `registry.list` 🟢 Read

List subkeys and values under a key.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `key` | string | Full key path |
| `depth` | integer (optional) | Subkey depth. Default: 1 (immediate children only) |

**Response:**
| Field | Type | Description |
|---|---|---|
| `key` | string | Key path |
| `subkeys` | array | `[{name, subkey_count, value_count}]` |
| `values` | array | `[{name, type, data}]` |
| `subkey_count` | integer | Total subkeys |
| `value_count` | integer | Total values |

**Implementation:** `RegistryKey.GetSubKeyNames()` + `GetValueNames()`

---

### Test Spec: registry.list
### `registry.list`

**Happy Path:**

- ✅ Lists subkeys and values for known key
- ✅ `depth: 1` only shows immediate children
- ✅ `depth: 2` shows grandchildren
- ✅ `subkey_count` and `value_count` are accurate
- ✅ Values include `name`, `type`, `data`

**Error Handling:**

- ✅ Key doesn't exist → error
- ✅ Access denied on subkey → skips, includes accessible ones

---

### Feature: registry.search
### `registry.search` 🟢 Read

Search registry by key name, value name, or data content.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `root_key` | string | Starting key path (e.g., "HKLM\SOFTWARE") |
| `query` | string | Search term |
| `search_in` | string (optional) | "keys" / "values" / "data" / "all". Default: "all" |
| `limit` | integer (optional) | Max results. Default: 50 |
| `max_depth` | integer (optional) | Max recursion depth. Default: 10 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `results` | array | `[{key, value_name, data, type, match_in}]` |
| `returned_count` | integer | Results returned |
| `truncated` | boolean | Whether limit was reached |
| `searched_keys` | integer | Keys searched |

**Implementation:** Recursive `RegistryKey` enumeration with string matching

---

### Test Spec: registry.search
### `registry.search`

**Happy Path:**

- ✅ Finds key by name match
- ✅ Finds value by name match
- ✅ Finds value by data content match
- ✅ `search_in: "keys"` only matches key names
- ✅ `search_in: "data"` only matches value data
- ✅ `limit` caps results
- ✅ `max_depth` limits recursion

**Error Handling:**

- ✅ No matches → empty results
- ✅ Access denied on subtree → skips, searches rest

**Security:**

- 🔒 Search root must not include blocked keys
- 🔒 Results do not include values from blocked keys

**Edge Cases:**

- ⚡ Search in large hive (`HKLM\SOFTWARE`) → respects limit and timeout
- ⚡ `query` with regex-like chars → treated as literal string
- ⚡ Binary data search → converts to hex/string for matching

---

### Feature: registry.set
### `registry.set` 🔴 Dangerous

Write a registry value.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `key` | string | Full key path |
| `value` | string | Value name |
| `data` | string / integer / array | Value data |
| `type` | string | "REG_SZ" / "REG_DWORD" / "REG_QWORD" / "REG_BINARY" / "REG_MULTI_SZ" / "REG_EXPAND_SZ" |
| `create_key` | boolean (optional) | Create key if it doesn't exist. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `key` | string | Key path |
| `value_name` | string | Value name |
| `previous_data` | any / null | Previous value if existed |
| `new_data` | any | Written data |
| `type` | string | Value type |
| `key_created` | boolean | Whether key was newly created |

**Security:** Blocked keys: `HKLM\SAM`, `HKLM\SECURITY`, `HKLM\SYSTEM\CurrentControlSet\Control\Lsa`. All writes are audited.

**Implementation:** `RegistryKey.SetValue()`

---

### Test Spec: registry.set
### `registry.set`

**Happy Path:**

- 🎭 Creates REG_SZ value → readable via `registry.get`
- 🎭 Creates REG_DWORD → integer stored correctly
- 🎭 Overwrites existing value → `previous_data` returned
- 🎭 `create_key: true` creates non-existent key
- 🎭 New value → `previous_data: null`

**Input Validation:**

- ✅ `type` not in valid list → error
- ✅ `data` type mismatch (string for DWORD) → error
- ✅ DWORD value > 4294967295 → error (overflow)
- ✅ QWORD value > 18446744073709551615 → error

**Security:**

- 🔒 Requires Dangerous tier
- 🔒 Blocked keys cannot be written
- 🔒 Cannot modify boot-critical keys without explicit policy
- 🔒 All writes logged with key, value name, and data in audit trail (data redacted if in sensitive key)
- 🔒 Cannot create keys in `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` (autostart) without elevated policy

**Error Handling:**

- ✅ Key doesn't exist and `create_key: false` → error
- ✅ Access denied → error
- ✅ Key is read-only (virtual registry) → error

**Edge Cases:**

- ⚡ Setting REG_MULTI_SZ with empty array
- ⚡ Setting REG_EXPAND_SZ with %SystemRoot% reference
- ⚡ Very long value name (>16383 chars — max for value name)
- ⚡ Concurrent writes to same value → last write wins

---

