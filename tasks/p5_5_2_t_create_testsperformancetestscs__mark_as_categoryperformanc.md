# Task: `[T]` Create `tests/PerformanceTests.cs` — (mark as `[Category("Performance")]`)

**Phase 5: Cross-Cutting Concerns & Integration**
**Sub-phase: 5.2 Performance Benchmarks**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` Create `tests/PerformanceTests.cs` — (mark as `[Category("Performance")]`)
  - ✅ `system.info` < 2s
  - ✅ `process.list` < 3s
  - ✅ `service.list` < 2s
  - ✅ `log.tail` (50 entries) < 1s
  - ✅ `network.interfaces` < 1s
  - ✅ `file.read` (1MB file) < 500ms
  - ✅ `registry.get` < 100ms
  - ✅ `ad.users` (100 users) < 5s
  - ✅ `printer.list` < 2s
  - ✅ Empty result tools < 500ms

## Tool Specifications

### Feature: ad.users
### `ad.users` 🔵 Domain

List or search AD users.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter` | string (optional) | Search by name, SAM, or email (contains match) |
| `ou` | string (optional) | Search in specific OU (DN format) |
| `enabled_only` | boolean (optional) | Only enabled accounts. Default: false |
| `limit` | integer (optional) | Max results. Default: 100 |
| `properties` | array (optional) | Additional LDAP attributes to return |

**Response:**
| Field | Type | Description |
|---|---|---|
| `users` | array | User objects |
| `returned_count` | integer | Results returned |

**User object:**
| Field | Type | Description |
|---|---|---|
| `sam_account_name` | string | Login name |
| `display_name` | string | Display name |
| `given_name` | string | First name |
| `surname` | string | Last name |
| `email` | string / null | Email address |
| `distinguished_name` | string | Full DN |
| `upn` | string | User Principal Name |
| `enabled` | boolean | Account enabled |
| `locked_out` | boolean | Account locked |
| `password_last_set` | string / null | ISO 8601 |
| `password_expired` | boolean | Whether password is expired |
| `last_logon` | string / null | ISO 8601 |
| `created` | string | ISO 8601 creation date |
| `modified` | string | ISO 8601 last modified |
| `ou` | string | Parent OU path |
| `member_of` | array | Direct group memberships (CN only) |
| `title` | string / null | Job title |
| `department` | string / null | Department |
| `company` | string / null | Company |
| `manager` | string / null | Manager DN |
| `telephone` | string / null | Phone number |

**Implementation:** `System.DirectoryServices.Protocols` LDAP query with filter `(&(objectClass=user)(objectCategory=person))`

---

### Test Spec: ad.users
### `ad.users`

**Happy Path:**

- 🧪 Returns users from Active Directory
- 🧪 `filter` matches by name, SAM, or email
- 🧪 `ou` restricts search to specific OU
- 🧪 `enabled_only: true` excludes disabled accounts
- 🧪 `limit` caps results
- 🧪 Each user has all required fields populated
- 🧪 `member_of` lists direct group memberships

**Error Handling:**

- ✅ Not domain-joined → error "Machine is not domain-joined"
- ✅ Domain controller unreachable → error with timeout
- ✅ Invalid OU DN → error "OU not found"
- ✅ Insufficient permissions → error "Access denied"

**Security:**

- 🔒 Requires Domain tier
- 🔒 LDAP injection in `filter` → sanitized (special chars escaped)
- 🔒 Cannot query password attributes (unicodePwd, supplementalCredentials)
- 🔒 `filter: "*)(objectClass=*"` (LDAP injection attempt) → escaped, returns safe results

**Edge Cases:**

- ⚡ Domain with >10,000 users → respects limit, uses paged LDAP
- ⚡ User with Unicode characters in name
- ⚡ User with empty email, department, title → null fields
- ⚡ User in nested OU → `ou` field shows full path
- ⚡ `properties` parameter requests custom LDAP attributes

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

### Feature: log.tail
### `log.tail` 🟢 Read

Get recent entries from a log channel.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `channel` | string (optional) | Log channel name (e.g., "Application", "System", "Security", "Microsoft-Windows-TaskScheduler/Operational"). Default: "System" |
| `lines` | integer (optional) | Number of recent entries. Default: 50 |
| `level` | string (optional) | Filter: "critical" / "error" / "warning" / "info" / "verbose" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `channel` | string | Channel name |
| `entries` | array | Log entry objects |
| `returned_count` | integer | Number of entries returned |

**Log entry:**
| Field | Type | Description |
|---|---|---|
| `timestamp` | string | ISO 8601 |
| `level` | string | Event level |
| `event_id` | integer | Event ID |
| `source` | string | Provider name |
| `message` | string | Formatted message |
| `computer` | string | Computer name |
| `user` | string / null | User SID or name |
| `task_category` | string / null | Task category |
| `keywords` | array | Event keywords |
| `correlation_id` | string / null | Activity/correlation GUID |
| `record_id` | integer | Record ID within channel |

**Implementation:** `EventLogReader` with `EventLogQuery` sorted by `TimeCreated` descending

---

### Test Spec: log.tail
### `log.tail`

**Happy Path:**

- ✅ Default (System channel) returns entries
- ✅ `channel: "Application"` returns Application log entries
- ✅ `channel: "Security"` returns Security log entries (if accessible)
- ✅ `lines: 5` returns at most 5 entries
- ✅ Entries ordered by timestamp descending
- ✅ `level: "error"` only returns error-level entries
- ✅ Each entry has all required fields
- ✅ `returned_count` matches `entries.length`

**Input Validation:**

- ✅ `lines` < 0 → error
- ✅ `lines` = 0 → empty array or error
- ✅ Invalid channel name → error "Channel not found"
- ✅ Invalid `level` value → error

**Error Handling:**

- ✅ Empty log channel → empty entries array
- ✅ Channel access denied (Security log without privilege) → error

**Edge Cases:**

- ⚡ Channel with > 1 million entries → returns last N efficiently, does not enumerate all
- ⚡ Entry with multi-line message
- ⚡ Entry with XML event data
- ⚡ Operational channel name with slashes (e.g., "Microsoft-Windows-TaskScheduler/Operational")

---

### Feature: network.interfaces
### `network.interfaces` 🟢 Read

List network interfaces with full configuration.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `interfaces` | array | NIC objects |
| `count` | integer | Total count |

**NIC object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Adapter name |
| `description` | string | Adapter description |
| `id` | string | Adapter GUID |
| `type` | string | "ethernet" / "wifi" / "loopback" / "tunnel" / "ppp" |
| `status` | string | "up" / "down" / "testing" / "unknown" |
| `mac_address` | string | MAC address |
| `speed_mbps` | integer | Link speed in Mbps |
| `ipv4_addresses` | array | `[{address, subnet, gateway}]` |
| `ipv6_addresses` | array | `[{address, prefix_length}]` |
| `dns_servers` | array | DNS server addresses |
| `dhcp_enabled` | boolean | Whether DHCP is active |
| `dhcp_server` | string / null | DHCP server address |
| `dns_suffix` | string / null | Connection-specific DNS suffix |
| `mtu` | integer | Maximum transmission unit |
| `bytes_sent` | integer | Total bytes sent |
| `bytes_received` | integer | Total bytes received |
| `packets_sent` | integer | Total packets sent |
| `packets_received` | integer | Total packets received |
| `errors_in` | integer | Inbound errors |
| `errors_out` | integer | Outbound errors |

**Implementation:** `NetworkInterface.GetAllNetworkInterfaces()` + `GetIPProperties()` + `GetIPStatistics()`

---

### Test Spec: network.interfaces
### `network.interfaces`

**Happy Path:**

- ✅ Returns non-empty list (at least loopback)
- ✅ Contains loopback interface with IP 127.0.0.1
- ✅ Each interface has `name`, `status`, `type`
- ✅ Active interface has non-empty `ipv4_addresses`
- ✅ MAC address format is "XX:XX:XX:XX:XX:XX" or "XX-XX-XX-XX-XX-XX"
- ✅ `speed_mbps` > 0 for connected interfaces
- ✅ Traffic counters (`bytes_sent`, `bytes_received`) are non-negative

**Edge Cases:**

- ⚡ VPN adapter (Tailscale, WireGuard) appears in list
- ⚡ Hyper-V virtual switch adapter
- ⚡ Interface with multiple IPv4 addresses
- ⚡ Interface with IPv6 only
- ⚡ Disconnected interface → `status: "down"`, null IP info
- ⚡ Interface with DHCP vs static → `dhcp_enabled` is correct

---

### Feature: printer.list
### `printer.list` 🟢 Read

List installed printers.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `include_network` | boolean (optional) | Include network printers. Default: true |
| `include_local` | boolean (optional) | Include local printers. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printers` | array | Printer objects |
| `count` | integer | Total count |
| `default_printer` | string / null | Name of default printer |

**Printer object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `share_name` | string / null | Share name if shared |
| `port_name` | string | Port (e.g., "USB001", "192.168.1.100", "LPT1") |
| `driver_name` | string | Driver name |
| `location` | string / null | Printer location |
| `comment` | string / null | Description/comment |
| `status` | string | "idle" / "printing" / "error" / "offline" / "paper_jam" / "paper_out" / "toner_low" / "warming_up" / "paused" |
| `is_default` | boolean | Default printer |
| `is_shared` | boolean | Shared on network |
| `is_network` | boolean | Network printer |
| `type` | string | "local" / "network" / "virtual" |
| `color` | boolean / null | Color capable |
| `duplex` | boolean / null | Duplex capable |
| `jobs_count` | integer | Current jobs in queue |

**Implementation:** WMI `Win32_Printer`

---

### Test Spec: printer.list
### `printer.list`

**Happy Path:**

- ✅ Returns list of installed printers
- ✅ `include_network: false` excludes network printers
- ✅ `include_local: false` excludes local printers
- ✅ `default_printer` identifies the default
- ✅ Each printer has `name`, `status`, `driver_name`
- ✅ `is_default: true` for exactly one printer (if default set)
- ✅ `jobs_count` reflects current queue depth
- ✅ `type` correctly distinguishes "local" / "network" / "virtual"

**Error Handling:**

- ✅ No printers installed → empty array
- ✅ Spooler service not running → error "Print Spooler not running"

**Edge Cases:**

- ⚡ "Microsoft Print to PDF" → `type: "virtual"`
- ⚡ "Microsoft XPS Document Writer" → `type: "virtual"`
- ⚡ Printer in error state → `status: "error"`
- ⚡ Offline printer → `status: "offline"`
- ⚡ Printer with no driver (corrupted) → `driver_name: null` or error string
- ⚡ Many printers (>50 on a print server)

---

### Feature: process.list
### `process.list` 🟢 Read

List all running processes.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `sort_by` | string (optional) | "cpu" / "memory" / "name" / "pid". Default: "pid" |
| `limit` | integer (optional) | Max results. Default: all |
| `filter_name` | string (optional) | Filter by process name (contains match) |
| `filter_user` | string (optional) | Filter by owning user |

**Response:**
| Field | Type | Description |
|---|---|---|
| `processes` | array | List of process objects |
| `total_count` | integer | Total process count |

**Process object:**
| Field | Type | Description |
|---|---|---|
| `pid` | integer | Process ID |
| `name` | string | Process name |
| `path` | string / null | Full executable path (null if access denied) |
| `user` | string / null | Owner username |
| `cpu_percent` | float | CPU usage percentage |
| `memory_mb` | float | Working set in MB |
| `memory_private_mb` | float | Private bytes in MB |
| `threads` | integer | Thread count |
| `handles` | integer | Handle count |
| `start_time` | string / null | ISO 8601 start time |
| `status` | string | "running" / "suspended" / "not_responding" |
| `parent_pid` | integer / null | Parent process ID |
| `priority` | string | "idle" / "below_normal" / "normal" / "above_normal" / "high" / "realtime" |
| `command_line` | string / null | Full command line (null if access denied) |

**Implementation:** `Process.GetProcesses()` + WMI `Win32_Process` for command line and owner

---

### Test Spec: process.list
### `process.list`

**Happy Path:**

- ✅ Returns non-empty `processes` array (system always has processes)
- ✅ Contains `System` process (PID 4)
- ✅ Contains `smss.exe` or equivalent system process
- ✅ `total_count` matches `processes.length`
- ✅ Each process has `pid` > 0 (except System Idle at 0)
- ✅ Each process has non-empty `name`
- ✅ Each process has non-negative `cpu_percent`
- ✅ Each process has non-negative `memory_mb`
- ✅ `sort_by: "cpu"` returns processes sorted by cpu_percent descending
- ✅ `sort_by: "memory"` returns processes sorted by memory_mb descending
- ✅ `sort_by: "name"` returns processes sorted alphabetically
- ✅ `sort_by: "pid"` returns processes sorted by PID ascending
- ✅ `limit: 5` returns exactly 5 processes
- ✅ `filter_name: "svchost"` returns only svchost processes
- ✅ `filter_name` is case-insensitive

**Error Handling:**

- ✅ Process exits between list and inspect → handle gracefully (stale data OK)
- ✅ Access denied for process details → `path: null`, `user: null`, `command_line: null`

**Edge Cases:**

- ⚡ `limit: 0` → returns empty array or error
- ⚡ `limit: 100000` → returns all processes, no crash
- ⚡ `filter_name` matches no processes → empty array, `total_count: 0`
- ⚡ `filter_user: "NT AUTHORITY\\SYSTEM"` returns system processes
- ⚡ Process with very long command line (>32000 chars)
- ⚡ Zombie/orphaned processes appear with correct parent_pid

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

### Feature: service.list
### `service.list` 🟢 Read

List all Windows services.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `status` | string (optional) | Filter: "running" / "stopped" / "paused" / "all". Default: "all" |
| `type` | string (optional) | Filter: "win32" / "driver" / "all". Default: "win32" |
| `filter_name` | string (optional) | Filter by service name or display name (contains match) |

**Response:**
| Field | Type | Description |
|---|---|---|
| `services` | array | List of service objects |
| `total_count` | integer | Total matching count |

**Service object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Service name (short name) |
| `display_name` | string | Display name |
| `status` | string | "running" / "stopped" / "paused" / "start_pending" / "stop_pending" / "continue_pending" / "pause_pending" |
| `startup_type` | string | "automatic" / "automatic_delayed" / "manual" / "disabled" / "boot" / "system" |
| `service_type` | string | "win32_own_process" / "win32_share_process" / "kernel_driver" / "file_system_driver" |
| `pid` | integer / null | Process ID if running |
| `account` | string | Service account (e.g., "LocalSystem", "NT AUTHORITY\\NETWORK SERVICE") |
| `path` | string | Binary path |
| `description` | string / null | Service description |
| `dependencies` | array | Service names this service depends on |
| `dependent_services` | array | Services that depend on this one |
| `can_stop` | boolean | Whether the service can be stopped |
| `can_pause` | boolean | Whether the service can be paused |

**Implementation:** `ServiceController.GetServices()` + WMI `Win32_Service` for extended fields

---

### Test Spec: service.list
### `service.list`

**Happy Path:**

- ✅ Returns non-empty `services` array
- ✅ Contains "Spooler" service (Print Spooler, exists on all Windows)
- ✅ Contains "W32Time" service (Windows Time)
- ✅ `total_count` matches `services.length`
- ✅ `status: "running"` only returns running services
- ✅ `status: "stopped"` only returns stopped services
- ✅ `type: "driver"` returns kernel and file system drivers
- ✅ `filter_name: "spooler"` is case-insensitive
- ✅ `filter_name` matches on both `name` and `display_name`
- ✅ Each service has all required fields populated

**Edge Cases:**

- ⚡ Service in transitional state (start_pending, stop_pending) during list
- ⚡ `filter_name` matches no services → empty array
- ⚡ `status: "paused"` on system with no paused services → empty array

---

### Feature: system.info
### `system.info` 🟢 Read

Returns comprehensive system information.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `hostname` | string | Machine hostname |
| `os_name` | string | e.g., "Microsoft Windows Server 2022 Datacenter" |
| `os_version` | string | e.g., "10.0.20348" |
| `os_build` | string | e.g., "20348.2340" |
| `architecture` | string | "x64" / "ARM64" |
| `cpu_name` | string | Processor model name |
| `cpu_cores` | integer | Physical core count |
| `cpu_logical` | integer | Logical processor count |
| `memory_total_mb` | integer | Total physical RAM in MB |
| `memory_available_mb` | integer | Available RAM in MB |
| `domain` | string | Domain name or "WORKGROUP" |
| `domain_role` | string | "StandaloneWorkstation" / "MemberServer" / "PrimaryDomainController" / etc. |
| `last_boot` | string | ISO 8601 timestamp of last boot |
| `timezone` | string | System timezone ID |
| `locale` | string | System locale (e.g., "en-CA") |
| `install_date` | string | OS install date |

**Implementation:** `Environment` properties + WMI `Win32_OperatingSystem`, `Win32_Processor`, `Win32_ComputerSystem`

---

### Test Spec: system.info
### `system.info`

**Happy Path:**

- ✅ Returns hostname matching `Environment.MachineName`
- ✅ Returns non-empty `os_name` containing "Windows"
- ✅ Returns `os_version` in format `X.Y.ZZZZZ`
- ✅ Returns `architecture` as "x64" or "ARM64"
- ✅ Returns `cpu_cores` > 0
- ✅ Returns `cpu_logical` >= `cpu_cores`
- ✅ Returns `memory_total_mb` > 0
- ✅ Returns `memory_available_mb` <= `memory_total_mb`
- ✅ Returns `memory_available_mb` > 0 (system is alive)
- ✅ Returns valid `domain` (either domain name or "WORKGROUP")
- ✅ Returns `last_boot` as valid ISO 8601 in the past
- ✅ Returns `timezone` as valid Windows timezone ID
- ✅ Returns `locale` in format "xx-XX"
- ✅ Returns `install_date` as valid ISO 8601 before current time
- 🧪 Returns `domain_role` matching actual machine role

**Error Handling:**

- ✅ WMI unavailable → returns partial result with available fields, errors noted
- 🎭 WMI timeout → returns error within 10 seconds, does not hang

**Edge Cases:**

- ⚡ Works on Windows Server Core (no GUI)
- ⚡ Works on Hyper-V guest with dynamic memory (memory values may change between calls)
- ⚡ Works on ARM64 Windows

---

