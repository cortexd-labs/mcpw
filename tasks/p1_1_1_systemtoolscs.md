# Task: SystemTools.cs

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.1 `system.*` (6 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **SystemTools.cs** — Implement all 6 tools using `IWmiClient` + `Environment` APIs
  - File: `src/Mcpw/Tools/SystemTools.cs`

## Tool Specifications

### Feature: system.*
## 1. `system.*` — System Info and Control

### Test Spec: system.*
## 1. `system.*`

### Feature: system.* — System Info and Control
## 1. `system.*` — System Info and Control

### Feature: system.env
### `system.env` 🟢 Read

List or get environment variables.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string (optional) | Specific variable name. If omitted, returns all. |
| `target` | string (optional) | "machine" / "user" / "process". Default: "process" |

**Response (single):**
| Field | Type | Description |
|---|---|---|
| `name` | string | Variable name |
| `value` | string | Variable value |
| `target` | string | Scope: machine / user / process |

**Response (all):**
| Field | Type | Description |
|---|---|---|
| `variables` | array | List of `{name, value, target}` objects |
| `count` | integer | Total count |

**Implementation:** `Environment.GetEnvironmentVariable()` with `EnvironmentVariableTarget`

---

### Test Spec: system.env
### `system.env`

**Happy Path (list all):**

- ✅ Returns non-empty `variables` array
- ✅ Contains `PATH` variable
- ✅ Contains `COMPUTERNAME` variable
- ✅ Contains `OS` variable with value "Windows_NT"
- ✅ `count` matches `variables.length`
- ✅ Each variable has non-empty `name`

**Happy Path (get specific):**

- ✅ `name: "PATH"` returns PATH value
- ✅ `name: "COMPUTERNAME"` returns machine hostname
- ✅ Name lookup is case-insensitive on Windows

**Input Validation:**

- ✅ Empty `name` (string "") → returns all variables
- ✅ `target` not in ["machine", "user", "process"] → error

**Error Handling:**

- ✅ Non-existent variable name → returns error or null value (not crash)

**Edge Cases:**

- ⚡ Variable with very long value (PATH can be >8000 chars)
- ⚡ Variable with Unicode characters in name or value
- ⚡ Variable with `=` in value (e.g., custom vars)
- ⚡ Variable with empty string value (exists but empty)
- ⚡ `target: "machine"` vs `target: "user"` returns different PATH values when they differ

---

### Feature: system.env.set
### `system.env.set` 🟡 Operate

Set an environment variable.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Variable name |
| `value` | string | Variable value |
| `target` | string (optional) | "machine" / "user". Default: "machine" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Variable name |
| `value` | string | New value |
| `target` | string | Scope applied |
| `previous_value` | string / null | Previous value if existed |

**Implementation:** `Environment.SetEnvironmentVariable()` with `EnvironmentVariableTarget.Machine`

---

### Test Spec: system.env.set
### `system.env.set`

**Happy Path:**

- ✅ Sets a new variable and `previous_value` is null
- ✅ Overwrites existing variable and `previous_value` has old value
- ✅ Variable persists across process restarts (machine scope)
- ✅ Returns correct `target` in response

**Input Validation:**

- ✅ Empty `name` → error
- ✅ Name with `=` → error (invalid variable name)
- ✅ Name with null byte → error
- ✅ `target` not in ["machine", "user"] → error (no "process" for persistence)

**Security:**

- 🔒 Cannot set `PATH` to include malicious directory without being in Operate tier
- 🔒 Cannot set `COMSPEC` or `PROCESSOR_ARCHITECTURE` (protected system vars)

**Edge Cases:**

- ⚡ Setting value to empty string removes the variable vs sets empty (OS behavior)
- ⚡ Variable name at max length (255 chars on Windows)
- ⚡ Variable value at max length (32767 chars)

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

### Feature: system.reboot
### `system.reboot` 🔴 Dangerous

Reboot, shutdown, or schedule a restart.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `action` | string | "reboot" / "shutdown" / "logoff" |
| `delay_seconds` | integer (optional) | Delay before action. Default: 0 |
| `reason` | string (optional) | Reason string for event log |
| `force` | boolean (optional) | Force close running applications. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `action` | string | Action initiated |
| `scheduled_at` | string | ISO 8601 timestamp when action will execute |
| `force` | boolean | Whether force was applied |

**Implementation:** `shutdown.exe /r /t {delay} /d p:0:0 /c "{reason}"` or WMI `Win32_OperatingSystem.Win32Shutdown()`

---

### Test Spec: system.reboot
### `system.reboot`

**Input Validation:**

- ✅ `action` not in ["reboot", "shutdown", "logoff"] → error
- ✅ `delay_seconds` < 0 → error
- ✅ `delay_seconds` > 315360000 (10 years) → error

**Happy Path:**

- 🎭 `action: "reboot"` calls correct Windows API
- 🎭 `action: "shutdown"` calls correct Windows API
- 🎭 `action: "logoff"` calls correct Windows API
- 🎭 `delay_seconds: 60` schedules delayed action
- 🎭 `force: true` sets force flag
- ✅ Returns `scheduled_at` in valid ISO 8601

**Security:**

- 🔒 Requires Dangerous privilege tier
- 🔒 Logged in audit trail with reason

**Edge Cases:**

- ⚡ Calling reboot while another reboot is already scheduled → error
- ⚡ `delay_seconds: 0` triggers immediately (response may not be received)

---

### Feature: system.sysctl
### `system.sysctl` 🟢 Read

Query Windows system configuration parameters (equivalent of Linux sysctl).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `category` | string (optional) | "memory" / "network" / "kernel" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `parameters` | array | List of `{name, value, category, description}` |

Returns mapped parameters: TCP window sizes, max connections, memory management settings, page file config, kernel debug settings — sourced from registry and WMI.

**Implementation:** Registry reads from `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters`, WMI `Win32_PageFileUsage`, `Win32_OperatingSystem` memory settings

---

### Test Spec: system.sysctl
### `system.sysctl`

**Happy Path:**

- ✅ `category: "all"` returns non-empty parameters array
- ✅ `category: "memory"` returns only memory-related parameters
- ✅ `category: "network"` returns TCP/IP parameters
- ✅ Each parameter has `name`, `value`, `category`, `description`

**Input Validation:**

- ✅ `category` not in valid list → error

**Error Handling:**

- ✅ Registry key access denied → skips parameter, includes in response with error note
- 🎭 Registry key doesn't exist → parameter not included (no error)

---

### Feature: system.uptime
### `system.uptime` 🟢 Read

Returns system uptime.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `uptime_seconds` | integer | Seconds since last boot |
| `uptime_human` | string | Human-readable (e.g., "4d 7h 23m") |
| `last_boot` | string | ISO 8601 timestamp |

**Implementation:** `Environment.TickCount64` or WMI `Win32_OperatingSystem.LastBootUpTime`

---

### Test Spec: system.uptime
### `system.uptime`

**Happy Path:**

- ✅ Returns `uptime_seconds` > 0
- ✅ Returns `uptime_human` in format "Xd Xh Xm"
- ✅ Returns `last_boot` as valid ISO 8601
- ✅ `uptime_seconds` is approximately consistent with `last_boot` (within 5 seconds tolerance)
- ✅ Multiple calls within 2 seconds return `uptime_seconds` values within 2 of each other

**Edge Cases:**

- ⚡ System uptime > 49.7 days (TickCount32 overflow boundary — must use TickCount64)
- ⚡ System uptime > 497 days (verify no overflow in seconds calculation)
- ⚡ Called immediately after boot (uptime < 60 seconds)

---

