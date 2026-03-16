# Task: ProcessTools.cs

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.2 `process.*` (6 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **ProcessTools.cs** — Implement using `Process` class + `IWmiClient` for extended data
  - File: `src/Mcpw/Tools/ProcessTools.cs`

## Tool Specifications

### Feature: process.*
## 2. `process.*` — Process Management

### Test Spec: process.*
## 2. `process.*`

### Feature: process.* — Process Management
## 2. `process.*` — Process Management

### Feature: process.inspect
### `process.inspect` 🟢 Read

Detailed info for a specific process.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `pid` | integer | Process ID |

**Response:** All fields from `process.list` process object plus:
| Field | Type | Description |
|---|---|---|
| `modules` | array | Loaded DLLs `[{name, path, size_bytes}]` |
| `environment` | object / null | Process environment variables (if accessible) |
| `io_reads` | integer | Total I/O read operations |
| `io_writes` | integer | Total I/O write operations |
| `io_read_bytes` | integer | Total bytes read |
| `io_write_bytes` | integer | Total bytes written |
| `gdi_objects` | integer | GDI object count |
| `user_objects` | integer | USER object count |
| `affinity_mask` | string | CPU affinity bitmask (hex) |
| `window_title` | string / null | Main window title if GUI process |
| `services` | array | Services hosted by this process (for svchost) |

**Implementation:** `Process.GetProcessById()` + `Process.Modules` + WMI `Win32_Process` + performance counters

---

### Test Spec: process.inspect
### `process.inspect`

**Happy Path:**

- ✅ Returns all fields for a known PID (e.g., own process)
- ✅ `modules` array is non-empty for a normal process
- ✅ `io_reads` and `io_writes` are non-negative
- ✅ `services` array is populated for svchost processes
- ✅ `window_title` is non-null for GUI processes

**Input Validation:**

- ✅ `pid` not an integer → error
- ✅ `pid` < 0 → error

**Error Handling:**

- ✅ PID doesn't exist → clear error "Process not found"
- ✅ PID exists but access denied → partial result with null fields
- ✅ Process exits during inspection → error or partial result (no crash)

**Edge Cases:**

- ⚡ Inspect PID 0 (System Idle) → returns minimal info
- ⚡ Inspect PID 4 (System) → returns info without user-level details
- ⚡ Inspect own process → full access to all fields
- ⚡ Process with no modules loaded (minimal process)
- ⚡ Process running as different user → limited info

---

### Feature: process.kill
### `process.kill` 🟡 Operate

Terminate a process.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `pid` | integer | Process ID to terminate |
| `force` | boolean (optional) | Kill entire process tree. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `pid` | integer | Process ID terminated |
| `name` | string | Process name |
| `killed` | boolean | Whether kill succeeded |
| `children_killed` | integer | Number of child processes killed (if force=true) |

**Implementation:** `Process.Kill()` / `Process.Kill(entireProcessTree: true)`

---

### Test Spec: process.kill
### `process.kill`

**Happy Path:**

- 🎭 Kills a test process successfully → `killed: true`
- 🎭 Returns correct `name` of killed process
- 🎭 `force: true` kills child processes → `children_killed` > 0

**Input Validation:**

- ✅ `pid` not an integer → error
- ✅ `pid` < 0 → error

**Error Handling:**

- ✅ PID doesn't exist → error "Process not found"
- ✅ PID is a protected system process (PID 0, 4) → error "Cannot kill system process"
- ✅ PID is a critical process (csrss, winlogon, lsass) → error "Cannot kill critical process"
- ✅ Access denied → error (not crash)
- ✅ Process already exited → error or success (idempotent)

**Security:**

- 🔒 Cannot kill PID 0 (System Idle)
- 🔒 Cannot kill PID 4 (System)
- 🔒 Cannot kill csrss.exe
- 🔒 Cannot kill winlogon.exe
- 🔒 Cannot kill lsass.exe
- 🔒 Cannot kill own parent process (neurond) — must be blocked explicitly
- 🔒 Cannot kill services.exe
- 🔒 Kill action is logged in audit trail

**Edge Cases:**

- ⚡ Kill a process that is in "not responding" state
- ⚡ Kill a process with many child processes (process tree)
- ⚡ Kill a process that holds a file lock
- ⚡ Rapid sequential kills of same PID → first succeeds, second fails gracefully

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

### Feature: process.nice
### `process.nice` 🟡 Operate

Change process priority.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `pid` | integer | Process ID |
| `priority` | string | "idle" / "below_normal" / "normal" / "above_normal" / "high" / "realtime" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `pid` | integer | Process ID |
| `name` | string | Process name |
| `previous_priority` | string | Previous priority level |
| `new_priority` | string | New priority level |

**Implementation:** `Process.PriorityClass = ProcessPriorityClass.{value}`

---

### Test Spec: process.nice
### `process.nice`

**Happy Path:**

- 🎭 Changes priority of test process → returns new and previous priority
- 🎭 All six priority levels are valid and accepted

**Input Validation:**

- ✅ `pid` not found → error
- ✅ `priority` not in valid list → error

**Security:**

- 🔒 Setting "realtime" priority requires Operate tier
- 🔒 Cannot change priority of protected system processes

**Edge Cases:**

- ⚡ Setting same priority as current → succeeds, `previous_priority` == `new_priority`
- ⚡ Process exits between validate and apply → error

---

### Feature: process.top
### `process.top` 🟢 Read

CPU and memory sorted process snapshot (like Task Manager).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `sort_by` | string (optional) | "cpu" / "memory". Default: "cpu" |
| `limit` | integer (optional) | Number of processes. Default: 20 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `processes` | array | Top N processes (same fields as `process.list`) |
| `system_cpu_percent` | float | Total system CPU usage |
| `system_memory_percent` | float | Total system memory usage |
| `system_memory_used_mb` | integer | Used physical memory |
| `system_memory_total_mb` | integer | Total physical memory |
| `process_count` | integer | Total running processes |
| `thread_count` | integer | Total threads |
| `handle_count` | integer | Total handles |

**Implementation:** WMI `Win32_PerfFormattedData_PerfProc_Process` + `Win32_PerfFormattedData_PerfOS_Processor`

---

### Test Spec: process.top
### `process.top`

**Happy Path:**

- ✅ Returns `processes` array with up to `limit` entries
- ✅ Default `limit: 20` returns 20 processes
- ✅ `sort_by: "cpu"` → first process has highest CPU
- ✅ `sort_by: "memory"` → first process has highest memory
- ✅ Returns valid `system_cpu_percent` between 0 and 100
- ✅ Returns valid `system_memory_percent` between 0 and 100
- ✅ `system_memory_used_mb` + available ≈ `system_memory_total_mb`
- ✅ `process_count` > 0
- ✅ `thread_count` >= `process_count`
- ✅ `handle_count` > 0

**Edge Cases:**

- ⚡ On idle system, top CPU process may show 0% (all below sample threshold)
- ⚡ `limit: 1` returns exactly 1 process
- ⚡ System under heavy load → cpu values add up reasonably (may exceed 100% on multicore)

---

### Feature: process.tree
### `process.tree` 🟢 Read

Process parent-child hierarchy.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `pid` | integer (optional) | Root PID. If omitted, shows full tree from PID 0. |
| `depth` | integer (optional) | Max tree depth. Default: unlimited |

**Response:**
| Field | Type | Description |
|---|---|---|
| `tree` | object | Recursive tree node |

**Tree node:**
| Field | Type | Description |
|---|---|---|
| `pid` | integer | Process ID |
| `name` | string | Process name |
| `user` | string / null | Owner |
| `cpu_percent` | float | CPU % |
| `memory_mb` | float | Working set MB |
| `children` | array | Child tree nodes |

**Implementation:** WMI `Win32_Process.ParentProcessId` → build tree in memory

---

### Test Spec: process.tree
### `process.tree`

**Happy Path:**

- ✅ No PID specified → returns full tree from root
- ✅ Tree includes known parent-child relationship (explorer.exe → child processes)
- ✅ `pid` specified → returns subtree rooted at that PID
- ✅ `depth: 1` → no grandchildren in tree
- ✅ Each node has `pid`, `name`, `children` array
- ✅ Leaf nodes have empty `children` array

**Error Handling:**

- ✅ PID not found → error
- ✅ Orphaned processes (parent died) → appear at top level

**Edge Cases:**

- ⚡ Process creates/destroys children during tree construction → partial tree OK
- ⚡ Very deep process tree (depth > 20)
- ⚡ Circular parent reference (should not happen, but handle gracefully)

---

