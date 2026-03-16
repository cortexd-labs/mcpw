# Task: `[R]` Self-review checklist

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.6 `file.*` (13 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` Self-review checklist

## Tool Specifications

### Feature: file.*
## 6. `file.*` — File Operations

### Test Spec: file.*
## 6. `file.*`

### Feature: file.* — File Operations
## 6. `file.*` — File Operations

### Feature: file.chmod
### `file.chmod` 🟡 Operate

Set file or directory ACL.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | File or directory path |
| `identity` | string | User or group (e.g., "DOMAIN\User", "BUILTIN\Administrators") |
| `rights` | string | "read" / "write" / "modify" / "full_control" |
| `type` | string | "allow" / "deny" |
| `inheritance` | string (optional) | "none" / "container" / "object" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Path modified |
| `rule_added` | object | `{identity, rights, type, inheritance}` |
| `current_acl` | array | Full updated ACL |

**Implementation:** `FileSystemSecurity.AddAccessRule()` / `SetAccessControl()`

---

### Test Spec: file.chmod
### `file.chmod`

**Happy Path:**

- 🎭 Adds allow read rule for specified identity
- 🎭 Returns updated ACL in response
- 🎭 Adds deny rule → denies access

**Input Validation:**

- ✅ Invalid `rights` value → error
- ✅ Invalid `type` → error
- ✅ Non-existent `identity` → error

**Security:**

- 🔒 Path within allowed prefixes only
- 🔒 Cannot change ACL on system files

---

### Feature: file.copy
### `file.copy` 🟡 Operate

Copy a file or directory.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `source` | string | Source path |
| `destination` | string | Destination path |
| `overwrite` | boolean (optional) | Overwrite if exists. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `source` | string | Source path |
| `destination` | string | Destination path |
| `bytes_copied` | integer | Total bytes copied |
| `files_copied` | integer | Files copied (>1 for directory copy) |

**Implementation:** `File.Copy()` / recursive `Directory` enumeration + copy

---

### Test Spec: file.copy
### `file.copy`

**Happy Path:**

- 🎭 Copies file to new location
- 🎭 `overwrite: true` replaces existing destination
- 🎭 Returns correct `bytes_copied`
- 🎭 Directory copy copies all contents

**Security:**

- 🔒 Both source and destination must be within allowed paths
- 🔒 Cannot copy to/from blocked paths

**Error Handling:**

- ✅ Source not found → error
- ✅ Destination exists and `overwrite: false` → error
- ✅ Insufficient disk space → error
- ✅ Source and destination are same file → error

---

### Feature: file.delete
### `file.delete` 🟡 Operate

Delete a file or directory.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | Path to delete |
| `recursive` | boolean (optional) | For directories, delete contents. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Deleted path |
| `deleted` | boolean | Success |
| `type` | string | "file" / "directory" |
| `items_deleted` | integer | Total items deleted (for recursive directory) |

**Implementation:** `File.Delete()` / `Directory.Delete(recursive)`

---

### Test Spec: file.delete
### `file.delete`

**Happy Path:**

- 🎭 Deletes file → `deleted: true`
- 🎭 Deletes empty directory → `deleted: true`
- 🎭 `recursive: true` deletes directory with contents

**Security:**

- 🔒 Path must be within allowed prefixes
- 🔒 Cannot delete allowed prefix root (e.g., `C:\Users`)
- 🔒 Cannot delete system files

**Error Handling:**

- ✅ Not found → error
- ✅ Non-empty directory without `recursive` → error
- ✅ File in use → error
- ✅ Read-only file → error (must remove attribute first)

---

### Feature: file.info
### `file.info` 🟢 Read

Get file or directory metadata.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | File or directory path |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Canonical path |
| `name` | string | File/directory name |
| `type` | string | "file" / "directory" / "symlink" |
| `size_bytes` | integer | File size (0 for directories) |
| `created` | string | ISO 8601 creation time |
| `modified` | string | ISO 8601 last modified |
| `accessed` | string | ISO 8601 last accessed |
| `attributes` | array | ["archive", "hidden", "readonly", "system", "compressed", "encrypted"] |
| `owner` | string | File owner (DOMAIN\user) |
| `acl` | array | `[{identity, type, rights, inherited}]` |
| `alternate_data_streams` | array | `[{name, size_bytes}]` — NTFS ADS |
| `is_symlink` | boolean | Whether path is a symbolic link |
| `symlink_target` | string / null | Symlink target path |

**Implementation:** `FileInfo` / `DirectoryInfo` + `FileSystemSecurity.GetAccessRules()` + `FindFirstStreamW` for ADS

---

### Test Spec: file.info
### `file.info`

**Happy Path:**

- ✅ Returns all fields for existing file
- ✅ Returns all fields for existing directory
- ✅ `type: "file"` for files, `type: "directory"` for dirs
- ✅ `acl` array is populated with access rules
- ✅ `owner` is in "DOMAIN\user" format
- ✅ `attributes` correctly identifies hidden, readonly, system files
- ✅ Symbolic link → `is_symlink: true` with `symlink_target`
- ✅ `alternate_data_streams` lists ADS if present

**Error Handling:**

- ✅ Path not found → error
- ✅ Access denied → error

**Security:**

- 🔒 Path validation same as `file.read`

**Edge Cases:**

- ⚡ File with no ADS → empty array
- ⚡ File with many ADS (>10)
- ⚡ Junction point → treated as symlink
- ⚡ Hard link → shows correct info, no special handling needed
- ⚡ Root directory (`C:\`) → valid response
- ⚡ Very long filename (255 chars)

---

### Feature: file.mkdir
### `file.mkdir` 🟡 Operate

Create a directory.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | Directory path to create |
| `recursive` | boolean (optional) | Create parent directories. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Created directory path |
| `created` | boolean | Whether directory was newly created (false if existed) |

**Implementation:** `Directory.CreateDirectory()`

---

### Test Spec: file.mkdir
### `file.mkdir`

**Happy Path:**

- 🎭 Creates directory → `created: true`
- 🎭 `recursive: true` creates nested path
- 🎭 Already exists → `created: false` (idempotent)

**Security:**

- 🔒 Path must be within allowed prefixes
- 🔒 Cannot create directories in system paths

**Error Handling:**

- ✅ Parent doesn't exist and `recursive: false` → error
- ✅ Path conflicts with existing file → error

---

### Feature: file.move
### `file.move` 🟡 Operate

Move or rename a file or directory.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `source` | string | Source path |
| `destination` | string | Destination path |
| `overwrite` | boolean (optional) | Overwrite if exists. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `source` | string | Original path |
| `destination` | string | New path |

**Implementation:** `File.Move()` / `Directory.Move()`

---

### Test Spec: file.move
### `file.move`

**Happy Path:**

- 🎭 Moves file → source gone, destination exists
- 🎭 `overwrite: true` replaces existing

**Security:**

- 🔒 Both source and destination within allowed paths

**Error Handling:**

- ✅ Cross-volume move → works (copy + delete internally)
- ✅ Destination exists, `overwrite: false` → error

---

### Feature: file.read
### `file.read` 🟢 Read

Read file contents.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | File path |
| `encoding` | string (optional) | "utf8" / "utf16" / "ascii" / "auto". Default: "auto" |
| `offset` | integer (optional) | Start reading at byte offset |
| `limit_bytes` | integer (optional) | Max bytes to read. Default: 1MB |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Canonical path |
| `content` | string | File contents |
| `size_bytes` | integer | Total file size |
| `encoding_detected` | string | Detected encoding |
| `truncated` | boolean | Whether content was truncated |

**Security:** Path must be within allowed prefixes. Blocked paths always rejected. Binary files return base64 with `encoding: "binary"`.

**Implementation:** `File.ReadAllText()` with `StreamReader` encoding detection

---

### Test Spec: file.read
### `file.read`

**Happy Path:**

- ✅ Reads UTF-8 text file correctly
- ✅ Reads UTF-16 (LE and BE) file correctly
- ✅ `encoding: "auto"` detects encoding from BOM
- ✅ `limit_bytes` truncates large files → `truncated: true`
- ✅ `offset` starts reading from specified byte
- ✅ `size_bytes` reflects total file size regardless of limit
- ✅ Returns `encoding_detected` matching actual encoding

**Input Validation:**

- ✅ Empty `path` → error
- ✅ Relative path → error (require absolute)
- ✅ `offset` < 0 → error
- ✅ `limit_bytes` < 0 → error

**Security:**

- 🔒 Path outside allowed prefixes → error "Access denied"
- 🔒 Blocked path (`C:\Windows\System32\config\SAM`) → error
- 🔒 Path traversal attempt (`C:\Users\..\Windows\System32\config\SAM`) → blocked after canonicalization
- 🔒 UNC path (`\\server\share\file`) → blocked (or explicitly allowed per config)
- 🔒 Symbolic link pointing outside allowed paths → blocked after resolution
- 🔒 Alternate data stream access (`file.txt:hidden`) → blocked
- 🔒 Device path (`\\.\PhysicalDrive0`) → blocked
- 🔒 Null byte in path (`C:\Users\file\x00.txt`) → rejected
- 🔒 Path with trailing dots/spaces (Windows auto-strips: `C:\secret.` → `C:\secret`) → validated after normalization

**Error Handling:**

- ✅ File not found → error "File not found"
- ✅ File locked by another process → error "File in use"
- ✅ Directory path (not a file) → error "Path is a directory"
- ✅ Permission denied (NTFS ACL) → error "Access denied"

**Edge Cases:**

- ⚡ Empty file (0 bytes) → `content: ""`, `size_bytes: 0`
- ⚡ Binary file → returns base64 with `encoding_detected: "binary"`
- ⚡ File exactly at `limit_bytes` → `truncated: false`
- ⚡ File with no BOM, mixed encoding → best-effort detection
- ⚡ File with very long lines (>1MB per line)
- ⚡ File path with Unicode characters (Chinese, Arabic, emoji)
- ⚡ File path at MAX_PATH (260 chars) and beyond (long path support)
- ⚡ File with read-only attribute → succeeds (reading doesn't need write)
- ⚡ File on network share (if UNC allowed)

---

### Feature: file.search
### `file.search` 🟢 Read

Search for files by name, pattern, or content.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | Root directory to search |
| `pattern` | string (optional) | Glob pattern (e.g., "_.log"). Default: "_" |
| `name_contains` | string (optional) | File name contains filter |
| `content_contains` | string (optional) | File content contains filter (slower) |
| `recursive` | boolean (optional) | Search subdirectories. Default: true |
| `min_size_bytes` | integer (optional) | Minimum file size |
| `max_size_bytes` | integer (optional) | Maximum file size |
| `modified_after` | string (optional) | ISO 8601 — modified after this time |
| `modified_before` | string (optional) | ISO 8601 — modified before this time |
| `type` | string (optional) | "file" / "directory" / "all". Default: "file" |
| `limit` | integer (optional) | Max results. Default: 100 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `root` | string | Search root path |
| `matches` | array | `[{path, name, size_bytes, modified, type}]` |
| `returned_count` | integer | Results returned |
| `truncated` | boolean | Whether limit was reached |

**Implementation:** `Directory.EnumerateFiles()` with `SearchOption.AllDirectories` + manual filters

---

### Test Spec: file.search
### `file.search`

**Happy Path:**

- ✅ `pattern: "*.log"` finds .log files
- ✅ `name_contains: "error"` finds files with "error" in name
- ✅ `content_contains: "Exception"` finds files containing that text
- ✅ `recursive: true` searches subdirectories
- ✅ `recursive: false` only searches immediate directory
- ✅ `min_size_bytes` and `max_size_bytes` filter correctly
- ✅ `modified_after` and `modified_before` filter by date
- ✅ `type: "directory"` returns only directories
- ✅ `limit` caps results → `truncated: true` if more exist

**Input Validation:**

- ✅ `path` not a directory → error
- ✅ `min_size_bytes` > `max_size_bytes` → error
- ✅ `modified_after` > `modified_before` → error

**Security:**

- 🔒 Search root must be within allowed paths
- 🔒 `content_contains` does not search blocked files
- 🔒 Results do not include files outside allowed paths (even if symlinked)

**Error Handling:**

- ✅ Empty directory → empty results
- ✅ Permission denied on subdirectory → skips it, includes rest

**Edge Cases:**

- ⚡ Directory with >100,000 files → respects limit, returns within timeout
- ⚡ Circular symlink → detected and skipped (not infinite loop)
- ⚡ `pattern` with special regex chars → treated as glob, not regex
- ⚡ `content_contains` on binary file → skipped or returns match position

---

### Feature: file.share
### `file.share` 🟢 Read

List SMB file shares.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `shares` | array | Share objects |
| `count` | integer | Total shares |

**Share object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Share name |
| `path` | string | Local path |
| `description` | string | Share description |
| `type` | string | "disk" / "print" / "ipc" / "special" |
| `max_connections` | integer | Max allowed connections |
| `current_connections` | integer | Active connections |
| `permissions` | array | `[{identity, access}]` |

**Implementation:** WMI `Win32_Share`

---

### Test Spec: file.share
### `file.share` / `file.share.create` / `file.share.remove`

**Happy Path:**

- ✅ `file.share` lists existing shares (at least IPC$, ADMIN$)
- 🎭 `file.share.create` creates a new share
- 🎭 `file.share.remove` removes a share

**Security:**

- 🔒 Cannot create share pointing outside allowed paths
- 🔒 Share creation requires Operate tier

---

### Feature: file.share.create
### `file.share.create` 🟡 Operate

Create an SMB file share.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Share name |
| `path` | string | Local directory path |
| `description` | string (optional) | Description |
| `max_connections` | integer (optional) | Max connections. Default: unlimited |
| `grant_read` | array (optional) | Identities with read access |
| `grant_full` | array (optional) | Identities with full access |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Share name |
| `path` | string | Local path |
| `created` | boolean | Success |
| `unc_path` | string | UNC path (\\hostname\sharename) |

**Implementation:** WMI `Win32_Share.Create()` or `New-SmbShare` via PowerShell

---

### Feature: file.share.remove
### `file.share.remove` 🟡 Operate

Remove an SMB file share.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Share name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Share name removed |
| `removed` | boolean | Success |

**Implementation:** WMI `Win32_Share.Delete()` or `Remove-SmbShare`

---

### Feature: file.tail
### `file.tail` 🟢 Read

Read last N lines of a file.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | File path |
| `lines` | integer (optional) | Number of lines. Default: 20 |
| `follow` | boolean (optional) | Stream new lines as appended. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | File path |
| `content` | string | Last N lines |
| `line_count` | integer | Lines returned |
| `file_size_bytes` | integer | Current file size |

**Implementation:** `FileStream` seek from end + read backwards to find N newlines. Follow mode uses `FileSystemWatcher`.

---

### Test Spec: file.tail
### `file.tail`

**Happy Path:**

- ✅ Returns last 20 lines by default
- ✅ `lines: 5` returns exactly 5 lines (if file has >= 5 lines)
- ✅ Returns correct `file_size_bytes`

**Error Handling:**

- ✅ File with fewer lines than requested → returns all lines
- ✅ Empty file → `content: ""`, `line_count: 0`

**Edge Cases:**

- ⚡ File with no trailing newline → last line still counted
- ⚡ Binary file → returns garbage (or error)
- ⚡ Very long lines (>1MB)
- ⚡ `follow: true` streams new lines (integration test)

---

### Feature: file.write
### `file.write` 🟡 Operate

Write content to a file.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | File path |
| `content` | string | Content to write |
| `encoding` | string (optional) | "utf8" / "utf16" / "ascii". Default: "utf8" |
| `mode` | string (optional) | "overwrite" / "append". Default: "overwrite" |
| `create_directories` | boolean (optional) | Create parent dirs if needed. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Canonical path |
| `bytes_written` | integer | Bytes written |
| `mode` | string | Write mode used |
| `created` | boolean | Whether file was newly created |

**Implementation:** `File.WriteAllText()` or `File.AppendAllText()`

---

### Test Spec: file.write
### `file.write`

**Happy Path:**

- 🎭 Creates new file → `created: true`
- 🎭 Overwrites existing file → `created: false`
- 🎭 `mode: "append"` adds to end of file
- 🎭 `create_directories: true` creates parent dirs
- 🎭 Returns correct `bytes_written`
- 🎭 Written content can be read back identically

**Input Validation:**

- ✅ Empty `path` → error
- ✅ Invalid `encoding` → error
- ✅ Invalid `mode` → error

**Security:**

- 🔒 Path outside allowed prefixes → blocked
- 🔒 Writing to system directories → blocked
- 🔒 Writing to executable extensions (.exe, .dll, .bat, .cmd, .ps1, .vbs) → blocked (configurable)
- 🔒 `create_directories: true` cannot create dirs outside allowed paths
- 🔒 Path traversal in `path` → blocked
- 🔒 Symbolic link target outside allowed paths → blocked

**Error Handling:**

- ✅ Disk full → error
- ✅ Parent directory doesn't exist and `create_directories: false` → error
- ✅ File is read-only → error
- ✅ File locked → error

**Edge Cases:**

- ⚡ Writing empty content → creates empty file
- ⚡ Very large content (>10MB) → succeeds or hits configured limit
- ⚡ Content with mixed line endings → preserved as-is
- ⚡ Concurrent writes to same file → last write wins (no corruption)

---

