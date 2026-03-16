# Task: ServiceTools.cs

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.3 `service.*` (8 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **ServiceTools.cs** — Implement using `IServiceControl` + `IWmiClient`
  - File: `src/Mcpw/Tools/ServiceTools.cs`

## Tool Specifications

### Feature: service.*
## 3. `service.*` — Service Management

### Test Spec: service.*
## 3. `service.*`

### Feature: service.* — Service Management
## 3. `service.*` — Service Management

### Feature: service.config
### `service.config` 🟢 Read

Get full service configuration details.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Service name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `display_name` | string | Display name |
| `binary_path` | string | Binary path with arguments |
| `account` | string | Logon account |
| `startup_type` | string | Startup type |
| `error_control` | string | "ignore" / "normal" / "severe" / "critical" |
| `load_order_group` | string / null | Load ordering group |
| `tag_id` | integer | Tag in load order group |
| `dependencies` | array | Dependency names |
| `recovery_actions` | array | `[{action, delay_ms}]` |
| `reset_period_seconds` | integer | Failure count reset period |
| `failure_command` | string / null | Command to run on failure |
| `delayed_auto_start` | boolean | Delayed auto-start flag |

**Implementation:** `QueryServiceConfig` + `QueryServiceConfig2` (recovery, delayed start, triggers)

---

### Test Spec: service.config
### `service.config`

**Happy Path:**

- ✅ Returns complete configuration for known service
- ✅ `binary_path` matches actual executable
- ✅ `recovery_actions` array has up to 3 actions (first, second, subsequent failures)
- ✅ `dependencies` are valid service names
- ✅ `delayed_auto_start` is boolean

**Error Handling:**

- ✅ Non-existent service → error

---

### Feature: service.enable
### `service.enable` 🟡 Operate

Change service startup type.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `startup_type` | string | "automatic" / "automatic_delayed" / "manual" / "disabled" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `previous_startup_type` | string | Previous startup type |
| `new_startup_type` | string | New startup type |

**Implementation:** WMI `Win32_Service.ChangeStartMode()` or `ChangeServiceConfig`

---

### Test Spec: service.enable
### `service.enable`

**Happy Path:**

- 🎭 Changes startup type and returns previous type
- 🎭 `startup_type: "disabled"` disables service
- 🎭 `startup_type: "automatic_delayed"` sets delayed auto-start

**Input Validation:**

- ✅ Invalid `startup_type` → error
- ✅ Non-existent service → error

**Security:**

- 🔒 Requires Operate privilege tier
- 🔒 Cannot disable critical boot services

**Edge Cases:**

- ⚡ Setting same startup type as current → succeeds, previous == new
- ⚡ Changing startup type of a running service (does not affect current state)

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

### Feature: service.logs
### `service.logs` 🟢 Read

Recent event log entries associated with a service.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `lines` | integer (optional) | Number of entries. Default: 50 |
| `level` | string (optional) | Filter: "error" / "warning" / "info" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `service_name` | string | Service name |
| `entries` | array | Log entry objects |

**Log entry:**
| Field | Type | Description |
|---|---|---|
| `timestamp` | string | ISO 8601 |
| `level` | string | "error" / "warning" / "info" / "verbose" |
| `event_id` | integer | Event ID |
| `message` | string | Event message |
| `source` | string | Event source |

**Implementation:** `EventLogQuery` with XPath filter on `System/Provider[@Name='{service}']`

---

### Test Spec: service.logs
### `service.logs`

**Happy Path:**

- ✅ Returns log entries for a known service
- ✅ `lines: 10` returns at most 10 entries
- ✅ `level: "error"` returns only error-level entries
- ✅ Entries are ordered by timestamp descending (newest first)
- ✅ Each entry has required fields (timestamp, level, event_id, message, source)

**Error Handling:**

- ✅ Service with no log entries → empty array
- ✅ Non-existent service → error or empty results

**Edge Cases:**

- ⚡ Service that logs to a custom event log channel
- ⚡ Very large event messages (>32KB)
- ⚡ Binary event data in message

---

### Feature: service.restart
### `service.restart` 🟡 Operate

Restart a service (stop + start).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `timeout_seconds` | integer (optional) | Total timeout for stop + start. Default: 60 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `stop_elapsed_ms` | integer | Time to stop |
| `start_elapsed_ms` | integer | Time to start |
| `current_status` | string | Final status |
| `pid` | integer / null | New process ID |

**Implementation:** Stop + WaitForStatus(Stopped) + Start + WaitForStatus(Running)

---

### Test Spec: service.restart
### `service.restart`

**Happy Path:**

- 🎭 Running service → stopped → started → `current_status: "running"`
- 🎭 Returns both `stop_elapsed_ms` and `start_elapsed_ms`
- 🎭 Returns new `pid` (different from before)

**Error Handling:**

- ✅ Service not running → starts it (or error, document behavior)
- ✅ Total timeout exceeded → error indicating which phase failed (stop or start)

**Edge Cases:**

- ⚡ Service that takes a long time to stop
- ⚡ Service that fails to restart after stop → error, service remains stopped

---

### Feature: service.start
### `service.start` 🟡 Operate

Start a stopped service.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `args` | array (optional) | Start arguments |
| `timeout_seconds` | integer (optional) | Wait timeout. Default: 30 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `previous_status` | string | Status before start |
| `current_status` | string | Status after start |
| `pid` | integer / null | New process ID |
| `elapsed_ms` | integer | Time to start |

**Implementation:** `ServiceController.Start()` + `WaitForStatus(Running, timeout)`

---

### Test Spec: service.start
### `service.start`

**Happy Path:**

- 🎭 Starting a stopped service → `current_status: "running"`
- 🎭 Returns `previous_status: "stopped"`
- 🎭 Returns valid `pid` for new process
- 🎭 `elapsed_ms` > 0

**Input Validation:**

- ✅ Empty `name` → error
- ✅ Non-existent service → error
- ✅ `timeout_seconds` < 0 → error

**Error Handling:**

- ✅ Already running → error "Service is already running"
- ✅ Disabled service → error "Service is disabled"
- ✅ Service dependencies not running → appropriate error
- ✅ Service start times out → error with timeout info
- 🎭 Service fails to start (crashes immediately) → error with event log info

**Security:**

- 🔒 Requires Operate privilege tier
- 🔒 Start action is logged in audit trail

**Edge Cases:**

- ⚡ Starting service with start arguments
- ⚡ Starting a service that has circular dependencies
- ⚡ Timeout of 1 second on slow-starting service → timeout error
- ⚡ Starting service whose binary is missing → error

---

### Feature: service.status
### `service.status` 🟢 Read

Detailed status of a specific service.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Service name (short name) |

**Response:** Same fields as service object in `service.list` plus:
| Field | Type | Description |
|---|---|---|
| `recovery_actions` | array | `[{action, delay_ms}]` — "restart" / "run_command" / "reboot" / "none" |
| `failure_count` | integer | Current failure count |
| `last_failure` | string / null | ISO 8601 timestamp of last failure |
| `triggers` | array | Service triggers (e.g., network available, domain join) |
| `delayed_auto_start` | boolean | Whether delayed auto-start is enabled |
| `sid_type` | string | "none" / "unrestricted" / "restricted" |
| `privileges` | array | Required privileges list |

**Implementation:** `ServiceController` + WMI `Win32_Service` + `QueryServiceConfig2` for recovery

---

### Test Spec: service.status
### `service.status`

**Happy Path:**

- ✅ Known running service → `status: "running"`
- ✅ Known stopped service → `status: "stopped"`
- ✅ Returns all extended fields (recovery_actions, triggers, etc.)
- ✅ `dependencies` lists correct service names
- ✅ `dependent_services` lists correct service names
- ✅ `can_stop` and `can_pause` are accurate

**Input Validation:**

- ✅ Empty `name` → error
- ✅ Non-existent service name → error "Service not found"

**Edge Cases:**

- ⚡ Service with no recovery actions configured
- ⚡ Service with no dependencies
- ⚡ Service in transitional state
- ⚡ Service name with spaces (e.g., "Windows Audio")

---

### Feature: service.stop
### `service.stop` 🟡 Operate

Stop a running service.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `force` | boolean (optional) | Stop dependent services too. Default: false |
| `timeout_seconds` | integer (optional) | Wait timeout. Default: 30 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Service name |
| `previous_status` | string | Status before stop |
| `current_status` | string | Status after stop |
| `dependents_stopped` | array | Names of dependent services stopped (if force=true) |
| `elapsed_ms` | integer | Time to stop |

**Implementation:** `ServiceController.Stop()` + `WaitForStatus(Stopped, timeout)`

---

### Test Spec: service.stop
### `service.stop`

**Happy Path:**

- 🎭 Stopping a running service → `current_status: "stopped"`
- 🎭 Returns `previous_status: "running"`
- 🎭 `force: true` stops dependent services
- 🎭 Returns list of dependents stopped

**Error Handling:**

- ✅ Already stopped → error "Service is not running"
- ✅ Service can't be stopped (`can_stop: false`) → error
- ✅ Service has running dependents and `force: false` → error listing dependents
- ✅ Stop times out → error
- 🎭 Service hangs on stop → timeout error

**Security:**

- 🔒 Cannot stop critical services: EventLog, RpcSs, Winmgmt (WMI), Netlogon (if DC)

**Edge Cases:**

- ⚡ Service that restarts automatically after stop (recovery = restart)
- ⚡ Stopping a paused service
- ⚡ Stopping a service in `stop_pending` state

---

