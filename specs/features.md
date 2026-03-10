# mcpw — Tool Reference

Complete reference for all mcpw MCP tools. Each tool is documented with its full input schema, response schema, example request/response, privilege tier, and Windows implementation details.

**Total: 17 domains, 130+ tools**

---

## Table of Contents

**Shared Domains (Linux parity)**

1. [system.\*](#1-system--system-info-and-control) — System Info and Control
2. [process.\*](#2-process--process-management) — Process Management
3. [service.\*](#3-service--service-management) — Service Management
4. [log.\*](#4-log--log-access) — Log Access
5. [network.\*](#5-network--network-stack) — Network Stack
6. [file.\*](#6-file--file-operations) — File Operations
7. [identity.\*](#7-identity--user-and-group-info) — User and Group Info
8. [storage.\*](#8-storage--disk-and-storage) — Disk and Storage
9. [schedule.\*](#9-schedule--scheduled-tasks) — Scheduled Tasks
10. [security.\*](#10-security--security-checks) — Security Checks
11. [container.\*](#11-container--container-management) — Container Management
12. [hardware.\*](#12-hardware--hardware-enumeration) — Hardware Enumeration
13. [time.\*](#13-time--time-and-ntp) — Time and NTP

**Windows-Specific Domains** 14. [registry.\*](#14-registry--windows-registry) — Windows Registry 15. [iis.\*](#15-iis--iis-web-server) — IIS Web Server 16. [ad.\*](#16-ad--active-directory) — Active Directory 17. [hyperv.\*](#17-hyperv--hyper-v-virtual-machines) — Hyper-V Virtual Machines 18. [gpo.\*](#18-gpo--group-policy) — Group Policy 19. [printer.\*](#19-printer--print-management) — Print Management

---

## Conventions

**Privilege Tiers:**

- 🟢 **Read** — `LOCAL SERVICE` or any user. No system changes.
- 🟡 **Operate** — Local Administrator. Modifies system state.
- 🔵 **Domain** — Domain account with delegated permissions.
- 🔴 **Dangerous** — Requires explicit neurond policy approval. Potentially destructive.

**Input parameters** use JSON Schema. All parameters are required unless marked `(optional)`.

**Response fields** document the JSON object returned in the MCP `tools/call` result.

---

## 1. `system.*` — System Info and Control

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

## 2. `process.*` — Process Management

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

## 3. `service.*` — Service Management

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

## 4. `log.*` — Log Access

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

### `log.search` 🟢 Read

Search logs by keyword, level, time range, event ID, or source.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `channel` | string (optional) | Channel name. Default: "System" |
| `query` | string (optional) | Keyword search in message text |
| `level` | string (optional) | "critical" / "error" / "warning" / "info" / "verbose" |
| `source` | string (optional) | Provider/source name filter |
| `event_id` | integer (optional) | Specific event ID |
| `since` | string (optional) | ISO 8601 start time |
| `until` | string (optional) | ISO 8601 end time |
| `limit` | integer (optional) | Max results. Default: 100 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `channel` | string | Channel searched |
| `entries` | array | Matching log entries (same structure as `log.tail`) |
| `returned_count` | integer | Results returned |
| `query_xpath` | string | Generated XPath query (for debugging) |

**Implementation:** `EventLogQuery` with constructed XPath filter combining all criteria

---

### `log.stream` 🟢 Read

Subscribe to new log entries in real time.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `channel` | string (optional) | Channel name. Default: "System" |
| `level` | string (optional) | Minimum level filter |
| `source` | string (optional) | Source filter |

**Response:** Streaming — emits log entry objects as they occur.

**Implementation:** `EventLogWatcher` with `EventRecordWritten` event subscription

---

### `log.channels` 🟢 Read

List available log channels.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter` | string (optional) | Filter channel names (contains match) |
| `include_empty` | boolean (optional) | Include channels with zero records. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `channels` | array | Channel objects |
| `total_count` | integer | Total count |

**Channel object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Channel name |
| `log_type` | string | "administrative" / "operational" / "analytic" / "debug" |
| `enabled` | boolean | Whether the channel is enabled |
| `record_count` | integer | Number of records |
| `max_size_kb` | integer | Maximum log size |
| `retention` | string | "overwrite" / "archive" / "none" |
| `file_path` | string | Log file path |

**Implementation:** `EventLogSession.GetLogNames()` + `EventLogConfiguration` for metadata

---

### `log.clear` 🟡 Operate

Clear a log channel.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `channel` | string | Channel name |
| `backup_path` | string (optional) | Path to save backup before clearing |

**Response:**
| Field | Type | Description |
|---|---|---|
| `channel` | string | Channel name |
| `records_cleared` | integer | Number of records removed |
| `backup_path` | string / null | Backup file path if saved |

**Implementation:** `EventLogSession.ClearLog()`

---

## 5. `network.*` — Network Stack

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

### `network.ports` 🟢 Read

List listening ports.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `protocol` | string (optional) | "tcp" / "udp" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `listeners` | array | Listener objects |
| `count` | integer | Total count |

**Listener object:**
| Field | Type | Description |
|---|---|---|
| `protocol` | string | "tcp" / "udp" |
| `local_address` | string | Bind address |
| `local_port` | integer | Bind port |
| `pid` | integer | Owning process ID |
| `process_name` | string | Process name |
| `state` | string | "listening" (TCP always listening for this tool) |

**Implementation:** `IPGlobalProperties.GetActiveTcpListeners()` + `GetActiveUdpListeners()` + `GetExtendedTcpTable` for PID mapping

---

### `network.connections` 🟢 Read

List active TCP/UDP connections.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `state` | string (optional) | Filter: "established" / "time_wait" / "close_wait" / "all". Default: "all" |
| `pid` | integer (optional) | Filter by process ID |
| `port` | integer (optional) | Filter by local or remote port |
| `limit` | integer (optional) | Max results. Default: 200 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `connections` | array | Connection objects |
| `count` | integer | Total matching |

**Connection object:**
| Field | Type | Description |
|---|---|---|
| `protocol` | string | "tcp" / "udp" |
| `local_address` | string | Local IP |
| `local_port` | integer | Local port |
| `remote_address` | string | Remote IP |
| `remote_port` | integer | Remote port |
| `state` | string | TCP state (e.g., "established", "time_wait") |
| `pid` | integer | Owning process ID |
| `process_name` | string | Process name |

**Implementation:** `IPGlobalProperties.GetActiveTcpConnections()` + `GetExtendedTcpTable` / `GetExtendedUdpTable`

---

### `network.dns` 🟢 Read

DNS configuration and resolution.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `resolve` | string (optional) | Hostname or IP to resolve |
| `type` | string (optional) | Record type: "A" / "AAAA" / "MX" / "CNAME" / "PTR". Default: "A" |

**Response (no resolve — config only):**
| Field | Type | Description |
|---|---|---|
| `hostname` | string | Machine hostname |
| `domain` | string | Primary DNS domain |
| `dns_servers` | array | Configured DNS servers (per interface) `[{interface, servers}]` |
| `search_suffixes` | array | DNS search suffix list |

**Response (with resolve):**
| Field | Type | Description |
|---|---|---|
| `query` | string | Queried name |
| `type` | string | Record type |
| `results` | array | Resolved records `[{address, ttl}]` or `[{exchange, priority}]` for MX |
| `elapsed_ms` | integer | Resolution time |

**Implementation:** `NetworkInterface.GetIPProperties().DnsAddresses` + `Dns.GetHostAddresses()` or PowerShell `Resolve-DnsName` for MX/CNAME

---

### `network.firewall` 🟢 Read

List Windows Firewall rules.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `profile` | string (optional) | "domain" / "private" / "public" / "all". Default: "all" |
| `direction` | string (optional) | "in" / "out" / "all". Default: "all" |
| `enabled_only` | boolean (optional) | Only show enabled rules. Default: true |
| `filter_name` | string (optional) | Filter by rule name (contains match) |

**Response:**
| Field | Type | Description |
|---|---|---|
| `rules` | array | Firewall rule objects |
| `count` | integer | Total matching |
| `firewall_enabled` | object | `{domain: bool, private: bool, public: bool}` |

**Firewall rule object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Rule name |
| `description` | string | Rule description |
| `enabled` | boolean | Whether rule is active |
| `direction` | string | "in" / "out" |
| `action` | string | "allow" / "block" |
| `protocol` | string | "tcp" / "udp" / "icmpv4" / "any" |
| `local_ports` | string | Port range or "any" |
| `remote_ports` | string | Port range or "any" |
| `local_addresses` | string | Address range or "any" |
| `remote_addresses` | string | Address range or "any" |
| `profiles` | array | ["domain", "private", "public"] |
| `program` | string / null | Application path |
| `service` | string / null | Service short name |
| `group` | string / null | Rule group |

**Implementation:** COM `INetFwPolicy2.Rules` enumeration

---

### `network.firewall.add` 🟡 Operate

Add a firewall rule.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Rule name |
| `direction` | string | "in" / "out" |
| `action` | string | "allow" / "block" |
| `protocol` | string | "tcp" / "udp" / "any" |
| `local_ports` | string (optional) | Port or range (e.g., "80", "8080-8090") |
| `remote_addresses` | string (optional) | IP or range. Default: "any" |
| `program` | string (optional) | Application path |
| `profile` | string (optional) | "domain" / "private" / "public" / "all". Default: "all" |
| `description` | string (optional) | Rule description |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Rule name created |
| `created` | boolean | Success |

**Implementation:** COM `INetFwPolicy2.Rules.Add()`

---

### `network.firewall.remove` 🟡 Operate

Remove a firewall rule.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Rule name to remove |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Rule removed |
| `removed` | boolean | Success |

**Implementation:** COM `INetFwPolicy2.Rules.Remove()`

---

### `network.routing` 🟢 Read

Show routing table.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `routes` | array | Route objects |
| `count` | integer | Total routes |

**Route object:**
| Field | Type | Description |
|---|---|---|
| `destination` | string | Destination network |
| `prefix_length` | integer | Subnet prefix length |
| `next_hop` | string | Gateway address |
| `interface_index` | integer | Interface index |
| `interface_alias` | string | Interface name |
| `metric` | integer | Route metric |
| `protocol` | string | "local" / "netmgmt" / "dhcp" / "static" |
| `type` | string | "unicast" / "broadcast" / "multicast" |

**Implementation:** `Get-NetRoute` via PowerShell SDK or `GetIpForwardTable2`

---

### `network.ping` 🟢 Read

Ping a host.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `host` | string | Hostname or IP address |
| `count` | integer (optional) | Number of pings. Default: 4 |
| `timeout_ms` | integer (optional) | Per-ping timeout. Default: 5000 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `host` | string | Target host |
| `resolved_address` | string | Resolved IP |
| `results` | array | `[{status, roundtrip_ms, ttl}]` |
| `sent` | integer | Packets sent |
| `received` | integer | Packets received |
| `lost` | integer | Packets lost |
| `loss_percent` | float | Loss percentage |
| `avg_ms` | float | Average roundtrip |
| `min_ms` | float | Minimum roundtrip |
| `max_ms` | float | Maximum roundtrip |

**Implementation:** `System.Net.NetworkInformation.Ping.SendPingAsync()`

---

### `network.traceroute` 🟢 Read

Trace route to a host.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `host` | string | Target hostname or IP |
| `max_hops` | integer (optional) | Maximum hops. Default: 30 |
| `timeout_ms` | integer (optional) | Per-hop timeout. Default: 5000 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `host` | string | Target host |
| `hops` | array | `[{hop, address, hostname, roundtrip_ms, status}]` |
| `reached` | boolean | Whether target was reached |

**Implementation:** `Ping.SendPingAsync()` with incrementing TTL

---

## 6. `file.*` — File Operations

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

## 7. `identity.*` — User and Group Info

### `identity.users` 🟢 Read

List local user accounts.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter_name` | string (optional) | Filter by username (contains) |
| `include_disabled` | boolean (optional) | Include disabled accounts. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `users` | array | User objects |
| `count` | integer | Total count |

**User object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Username |
| `full_name` | string | Full name |
| `description` | string | Description |
| `sid` | string | Security identifier |
| `enabled` | boolean | Account enabled |
| `locked_out` | boolean | Account locked |
| `password_required` | boolean | Password required flag |
| `password_last_set` | string / null | ISO 8601 |
| `last_logon` | string / null | ISO 8601 last logon time |
| `groups` | array | Group memberships |
| `home_directory` | string / null | Home directory path |
| `logon_script` | string / null | Logon script path |

**Implementation:** WMI `Win32_UserAccount` + `DirectoryEntry` for SAM details

---

### `identity.groups` 🟢 Read

List local groups.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter_name` | string (optional) | Filter by group name (contains) |

**Response:**
| Field | Type | Description |
|---|---|---|
| `groups` | array | Group objects |
| `count` | integer | Total count |

**Group object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Group name |
| `description` | string | Description |
| `sid` | string | Security identifier |
| `members` | array | Member names |
| `member_count` | integer | Total members |

**Implementation:** WMI `Win32_Group` + `Win32_GroupUser`

---

### `identity.whoami` 🟢 Read

Current user context and privileges.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `username` | string | DOMAIN\username |
| `sid` | string | User SID |
| `is_admin` | boolean | Whether running as administrator |
| `is_system` | boolean | Whether running as SYSTEM |
| `groups` | array | Group memberships with SIDs |
| `privileges` | array | Assigned privileges `[{name, enabled}]` |
| `logon_type` | string | "interactive" / "service" / "network" / "batch" |
| `elevation_type` | string | "default" / "full" / "limited" |
| `integrity_level` | string | "low" / "medium" / "high" / "system" |

**Implementation:** `WindowsIdentity.GetCurrent()` + `WindowsPrincipal.IsInRole()` + `GetTokenInformation`

---

### `identity.user.create` 🟡 Operate

Create a local user account.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Username |
| `password` | string | Initial password |
| `full_name` | string (optional) | Full name |
| `description` | string (optional) | Description |
| `groups` | array (optional) | Group names to add user to |
| `must_change_password` | boolean (optional) | Force password change on first logon. Default: false |
| `cannot_change_password` | boolean (optional) | Prevent user from changing password. Default: false |
| `password_never_expires` | boolean (optional) | Password never expires. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Username created |
| `sid` | string | SID |
| `groups` | array | Group memberships |
| `created` | boolean | Success |

**Implementation:** `DirectoryEntry` SAM provider or `net user /add`

---

### `identity.user.delete` 🔴 Dangerous

Delete a local user account.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Username to delete |
| `delete_profile` | boolean (optional) | Delete user profile directory. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Username deleted |
| `profile_deleted` | boolean | Whether profile was deleted |

**Implementation:** `DirectoryEntry.DeleteTree()` + `WMI Win32_UserProfile.Delete()`

---

## 8. `storage.*` — Disk and Storage

### `storage.disks` 🟢 Read

List physical disks.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `disks` | array | Disk objects |
| `count` | integer | Total count |

**Disk object:**
| Field | Type | Description |
|---|---|---|
| `index` | integer | Disk number |
| `model` | string | Disk model |
| `serial` | string | Serial number |
| `size_bytes` | integer | Total capacity |
| `size_human` | string | Human-readable size |
| `media_type` | string | "HDD" / "SSD" / "NVMe" / "Unknown" |
| `interface` | string | "SATA" / "NVMe" / "SAS" / "USB" |
| `partitions` | integer | Partition count |
| `status` | string | "OK" / "Degraded" / "Error" |
| `firmware_version` | string | Firmware version |
| `bus_type` | string | Bus type |

**Implementation:** WMI `Win32_DiskDrive` + `MSFT_PhysicalDisk` (Storage Spaces)

---

### `storage.volumes` 🟢 Read

List volumes and mount points.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `volumes` | array | Volume objects |
| `count` | integer | Total count |

**Volume object:**
| Field | Type | Description |
|---|---|---|
| `drive_letter` | string / null | Drive letter (e.g., "C:") |
| `label` | string | Volume label |
| `file_system` | string | "NTFS" / "ReFS" / "FAT32" / "exFAT" |
| `size_bytes` | integer | Total capacity |
| `free_bytes` | integer | Free space |
| `used_bytes` | integer | Used space |
| `used_percent` | float | Usage percentage |
| `type` | string | "fixed" / "removable" / "network" / "cdrom" / "ram" |
| `mount_point` | string | Mount point path |
| `serial_number` | string | Volume serial number |
| `compressed` | boolean | Compression enabled |
| `bitlocker_status` | string / null | "encrypted" / "decrypted" / "encrypting" / null |

**Implementation:** `DriveInfo.GetDrives()` + WMI `Win32_Volume` + `Win32_EncryptableVolume` for BitLocker

---

### `storage.usage` 🟢 Read

Disk space usage summary.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string (optional) | Get usage for specific path. If omitted, all drives. |

**Response:**
| Field | Type | Description |
|---|---|---|
| `drives` | array | `[{letter, label, total_gb, used_gb, free_gb, used_percent}]` |
| `total_gb` | float | Total capacity across all drives |
| `used_gb` | float | Total used |
| `free_gb` | float | Total free |

**Implementation:** `DriveInfo.GetDrives()`

---

### `storage.smart` 🟢 Read

SMART health data for physical disks.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `disk` | integer (optional) | Disk index. If omitted, all disks. |

**Response:**
| Field | Type | Description |
|---|---|---|
| `disks` | array | SMART data per disk |

**SMART object:**
| Field | Type | Description |
|---|---|---|
| `disk_index` | integer | Disk number |
| `model` | string | Disk model |
| `status` | string | "OK" / "Caution" / "Bad" |
| `temperature_celsius` | integer / null | Current temperature |
| `power_on_hours` | integer / null | Total power-on hours |
| `reallocated_sectors` | integer / null | Reallocated sector count |
| `pending_sectors` | integer / null | Pending sector count |
| `attributes` | array | `[{id, name, value, worst, threshold, raw}]` |

**Implementation:** WMI `MSStorageDriver_ATAPISmartData` + `MSStorageDriver_FailurePredictStatus`

---

### `storage.partitions` 🟢 Read

List disk partitions.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `disk` | integer (optional) | Disk index. If omitted, all disks. |

**Response:**
| Field | Type | Description |
|---|---|---|
| `partitions` | array | Partition objects |

**Partition object:**
| Field | Type | Description |
|---|---|---|
| `disk_index` | integer | Parent disk |
| `index` | integer | Partition number |
| `type` | string | "GPT" / "MBR" |
| `gpt_type` | string / null | GPT type GUID name (e.g., "EFI System", "Basic Data") |
| `size_bytes` | integer | Partition size |
| `offset_bytes` | integer | Starting offset |
| `drive_letter` | string / null | Assigned drive letter |
| `is_boot` | boolean | Boot partition |
| `is_active` | boolean | Active partition |

**Implementation:** WMI `Win32_DiskPartition` + `MSFT_Partition`

---

## 9. `schedule.*` — Scheduled Tasks

### `schedule.list` 🟢 Read

List scheduled tasks.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `folder` | string (optional) | Task folder path. Default: "\" (root) |
| `recursive` | boolean (optional) | Include subfolders. Default: true |
| `include_disabled` | boolean (optional) | Include disabled tasks. Default: true |
| `include_microsoft` | boolean (optional) | Include Microsoft tasks. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `tasks` | array | Task objects |
| `count` | integer | Total count |

**Task object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Task name |
| `path` | string | Full task path |
| `enabled` | boolean | Whether enabled |
| `status` | string | "ready" / "running" / "disabled" / "has_not_run" / "no_more_runs" |
| `last_run` | string / null | ISO 8601 last run time |
| `last_result` | integer | Last exit code |
| `next_run` | string / null | ISO 8601 next scheduled run |
| `author` | string / null | Task author |
| `description` | string / null | Task description |
| `triggers` | array | `[{type, schedule, enabled}]` |
| `actions` | array | `[{type, command, arguments, working_dir}]` |
| `run_as` | string | Account the task runs as |
| `run_level` | string | "limited" / "highest" |

**Implementation:** COM `ITaskService` → `ITaskFolder.GetTasks()` recursive

---

### `schedule.info` 🟢 Read

Detailed info for a specific task.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | Full task path (e.g., "\MyBackup" or "\Microsoft\Windows\Defrag\ScheduledDefrag") |

**Response:** All fields from task object plus:
| Field | Type | Description |
|---|---|---|
| `triggers_detail` | array | Full trigger definitions with intervals, repetition, start/end boundaries |
| `conditions` | object | `{idle_only, network_required, ac_power_only, wake_to_run}` |
| `settings` | object | `{allow_on_demand, stop_on_idle, restart_on_failure, restart_count, restart_interval, execution_time_limit, delete_expired, priority, hidden, multiple_instances}` |
| `history_count` | integer | Number of history entries |
| `registration_date` | string | ISO 8601 when task was registered |

**Implementation:** COM `ITaskService.GetFolder().GetTask()` → full `ITaskDefinition`

---

### `schedule.add` 🟡 Operate

Create a scheduled task.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Task name |
| `folder` | string (optional) | Folder path. Default: "\" |
| `command` | string | Executable path |
| `arguments` | string (optional) | Command arguments |
| `working_dir` | string (optional) | Working directory |
| `trigger_type` | string | "daily" / "weekly" / "monthly" / "once" / "boot" / "logon" / "idle" |
| `start_time` | string (optional) | ISO 8601 start time (for time-based triggers) |
| `interval_days` | integer (optional) | Repeat interval in days (for daily) |
| `days_of_week` | array (optional) | For weekly: ["monday", "wednesday", "friday"] |
| `run_as` | string (optional) | User account. Default: "SYSTEM" |
| `run_level` | string (optional) | "limited" / "highest". Default: "highest" |
| `description` | string (optional) | Task description |
| `enabled` | boolean (optional) | Create enabled. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Task name |
| `path` | string | Full task path |
| `created` | boolean | Success |
| `next_run` | string / null | Next scheduled run |

**Implementation:** COM `ITaskService.NewTask()` → configure triggers/actions → `ITaskFolder.RegisterTaskDefinition()`

---

### `schedule.remove` 🟡 Operate

Delete a scheduled task.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | Full task path |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Task path removed |
| `removed` | boolean | Success |

**Implementation:** COM `ITaskFolder.DeleteTask()`

---

### `schedule.run` 🟡 Operate

Manually trigger a scheduled task to run now.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | Full task path |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Task path |
| `triggered` | boolean | Whether run was initiated |
| `status` | string | Current task status |

**Implementation:** COM `IRegisteredTask.Run()`

---

### `schedule.enable` 🟡 Operate

Enable or disable a scheduled task.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string | Full task path |
| `enabled` | boolean | true = enable, false = disable |

**Response:**
| Field | Type | Description |
|---|---|---|
| `path` | string | Task path |
| `enabled` | boolean | New state |
| `previous_enabled` | boolean | Previous state |

**Implementation:** COM `IRegisteredTask.Enabled = value`

---

## 10. `security.*` — Security Checks

### `security.certs` 🟢 Read

List certificates in Windows certificate stores.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `store` | string (optional) | "my" / "root" / "ca" / "trustedpeople" / "trustedpublisher". Default: "my" |
| `location` | string (optional) | "machine" / "user". Default: "machine" |
| `expiring_within_days` | integer (optional) | Only show certs expiring within N days |

**Response:**
| Field | Type | Description |
|---|---|---|
| `certificates` | array | Certificate objects |
| `count` | integer | Total count |

**Certificate object:**
| Field | Type | Description |
|---|---|---|
| `subject` | string | Subject DN |
| `issuer` | string | Issuer DN |
| `thumbprint` | string | SHA-1 thumbprint |
| `serial_number` | string | Serial number |
| `not_before` | string | ISO 8601 valid from |
| `not_after` | string | ISO 8601 valid to |
| `days_until_expiry` | integer | Days until expiration |
| `is_expired` | boolean | Whether expired |
| `has_private_key` | boolean | Whether private key is available |
| `key_algorithm` | string | Key algorithm (RSA, ECDSA) |
| `key_size` | integer | Key size in bits |
| `san` | array | Subject Alternative Names |
| `eku` | array | Enhanced Key Usages |
| `self_signed` | boolean | Whether self-signed |

**Implementation:** `X509Store` + `X509Certificate2` enumeration

---

### `security.open_ports` 🟢 Read

Alias for `network.ports`. Lists all listening ports with owning process.

---

### `security.audit_policy` 🟢 Read

Show current audit policy settings.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `categories` | array | `[{category, subcategory, success, failure}]` |

Shows audit settings for: Account Logon, Account Management, Logon/Logoff, Object Access, Policy Change, Privilege Use, System, etc.

**Implementation:** `auditpol /get /category:*` parsed or `AuditEnumerateSubCategories` + `AuditQuerySystemPolicy`

---

### `security.local_policy` 🟢 Read

Show security-relevant local policies.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `password_policy` | object | `{min_length, max_age_days, history_count, complexity_required, lockout_threshold, lockout_duration_minutes}` |
| `user_rights` | array | `[{right, accounts}]` — who has SeDebugPrivilege, SeRemoteShutdownPrivilege, etc. |
| `security_options` | array | `[{name, value}]` — relevant security settings |

**Implementation:** `net accounts` parsed + `secedit /export` + `LsaEnumerateAccountsWithUserRight`

---

### `security.windows_update` 🟢 Read

Check Windows Update status and installed updates.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `pending_only` | boolean (optional) | Only show pending updates. Default: false |
| `limit` | integer (optional) | Max installed updates to return. Default: 50 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `last_check` | string / null | ISO 8601 last update check |
| `last_install` | string / null | ISO 8601 last update install |
| `reboot_required` | boolean | Whether reboot is pending |
| `pending_updates` | array | `[{title, kb, severity, size_mb}]` |
| `installed_updates` | array | `[{title, kb, installed_on, type}]` |

**Implementation:** COM `IUpdateSearcher` (Windows Update Agent API) + WMI `Win32_QuickFixEngineering`

---

### `security.defender` 🟢 Read

Windows Defender / antivirus status.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `antivirus_enabled` | boolean | Real-time protection on |
| `product_name` | string | AV product name |
| `engine_version` | string | Engine version |
| `definition_version` | string | Definition/signature version |
| `definition_date` | string | ISO 8601 last definition update |
| `last_scan` | string / null | ISO 8601 last scan time |
| `last_scan_type` | string | "quick" / "full" / "custom" |
| `threats_detected` | integer | Active threat count |
| `recent_threats` | array | `[{name, severity, status, detected_on}]` |

**Implementation:** WMI `root/Microsoft/Windows/Defender` namespace + `Get-MpComputerStatus` via PowerShell

---

## 11. `container.*` — Container Management

### `container.list` 🟢 Read

List Docker containers.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `all` | boolean (optional) | Include stopped containers. Default: true |
| `filter_name` | string (optional) | Filter by name (contains) |
| `filter_image` | string (optional) | Filter by image name |
| `filter_status` | string (optional) | "running" / "exited" / "paused" / "created" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `containers` | array | Container objects |
| `count` | integer | Total count |

**Container object:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID (short) |
| `name` | string | Container name |
| `image` | string | Image name:tag |
| `status` | string | Human-readable status |
| `state` | string | "running" / "exited" / "paused" / "created" / "restarting" / "dead" |
| `created` | string | ISO 8601 creation time |
| `ports` | array | `[{host_port, container_port, protocol, host_ip}]` |
| `networks` | array | Network names |
| `mounts` | array | `[{source, destination, type}]` |
| `labels` | object | Container labels |
| `exit_code` | integer / null | Exit code if exited |

**Implementation:** Docker Engine API via Windows named pipe `//./pipe/docker_engine`

---

### `container.inspect` 🟢 Read

Detailed container information.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |

**Response:** All fields from container object plus:
| Field | Type | Description |
|---|---|---|
| `full_id` | string | Full container ID |
| `command` | string | Entrypoint + command |
| `environment` | array | Environment variables |
| `restart_policy` | object | `{name, max_retries}` |
| `resource_limits` | object | `{cpu_shares, memory_limit_bytes, memory_reservation_bytes}` |
| `health_check` | object / null | `{test, interval, timeout, retries, status}` |
| `ip_address` | string | Container IP |
| `mac_address` | string | Container MAC |
| `log_driver` | string | Log driver name |
| `platform` | string | "linux" / "windows" |

**Implementation:** Docker API `GET /containers/{id}/json`

---

### `container.logs` 🟢 Read

Get container logs.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `tail` | integer (optional) | Last N lines. Default: 100 |
| `since` | string (optional) | ISO 8601 start time |
| `until` | string (optional) | ISO 8601 end time |
| `stdout` | boolean (optional) | Include stdout. Default: true |
| `stderr` | boolean (optional) | Include stderr. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `container` | string | Container ID |
| `logs` | string | Log output |
| `line_count` | integer | Lines returned |

**Implementation:** Docker API `GET /containers/{id}/logs`

---

### `container.exec` 🟡 Operate

Execute a command inside a running container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `command` | array | Command + arguments (e.g., ["ls", "-la"]) |
| `user` | string (optional) | User to run as |
| `working_dir` | string (optional) | Working directory inside container |
| `env` | array (optional) | Additional environment variables ["KEY=VALUE"] |

**Response:**
| Field | Type | Description |
|---|---|---|
| `exit_code` | integer | Command exit code |
| `stdout` | string | Standard output |
| `stderr` | string | Standard error |

**Implementation:** Docker API `POST /containers/{id}/exec` + `POST /exec/{id}/start`

---

### `container.start` 🟡 Operate

Start a stopped container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID |
| `name` | string | Container name |
| `started` | boolean | Success |
| `state` | string | New state |

**Implementation:** Docker API `POST /containers/{id}/start`

---

### `container.stop` 🟡 Operate

Stop a running container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `timeout_seconds` | integer (optional) | Grace period before kill. Default: 10 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID |
| `name` | string | Container name |
| `stopped` | boolean | Success |
| `state` | string | New state |

**Implementation:** Docker API `POST /containers/{id}/stop`

---

### `container.restart` 🟡 Operate

Restart a container.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | Container ID or name |
| `timeout_seconds` | integer (optional) | Grace period. Default: 10 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `id` | string | Container ID |
| `name` | string | Container name |
| `restarted` | boolean | Success |

**Implementation:** Docker API `POST /containers/{id}/restart`

---

### `container.images` 🟢 Read

List Docker images.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter_name` | string (optional) | Filter by repository name |
| `dangling` | boolean (optional) | Only show dangling images. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `images` | array | `[{id, repository, tag, created, size_mb, containers}]` |
| `count` | integer | Total images |

**Implementation:** Docker API `GET /images/json`

---

## 12. `hardware.*` — Hardware Enumeration

### `hardware.pci` 🟢 Read

List PCI devices.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `class` | string (optional) | Filter by device class: "display" / "network" / "storage" / "audio" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `devices` | array | PCI device objects |
| `count` | integer | Total count |

**Device object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Device name |
| `description` | string | Description |
| `manufacturer` | string | Manufacturer |
| `device_id` | string | Hardware ID (VEN_XXXX&DEV_XXXX) |
| `class` | string | Device class |
| `driver_version` | string / null | Installed driver version |
| `driver_date` | string / null | Driver date |
| `status` | string | "ok" / "error" / "disabled" |
| `location` | string | Bus/device/function location |

**Implementation:** WMI `Win32_PnPEntity` with `PNPClass` filter

---

### `hardware.usb` 🟢 Read

List USB devices.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `devices` | array | USB device objects |
| `count` | integer | Total count |

**USB device object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Device name |
| `device_id` | string | USB VID/PID |
| `manufacturer` | string / null | Manufacturer |
| `description` | string | Description |
| `status` | string | "ok" / "error" |
| `class` | string | Device class (e.g., "HID", "Storage", "Printer") |
| `hub_port` | string / null | USB hub and port info |

**Implementation:** WMI `Win32_USBControllerDevice` + `Win32_PnPEntity`

---

### `hardware.bios` 🟢 Read

BIOS/UEFI and motherboard information.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `bios_vendor` | string | BIOS manufacturer |
| `bios_version` | string | BIOS version string |
| `bios_date` | string | BIOS release date |
| `bios_mode` | string | "UEFI" / "Legacy" |
| `secure_boot` | boolean | Secure Boot enabled |
| `motherboard_manufacturer` | string | Board manufacturer |
| `motherboard_product` | string | Board product name |
| `motherboard_serial` | string | Board serial |
| `system_manufacturer` | string | System manufacturer |
| `system_model` | string | System model |
| `system_serial` | string | System serial number |
| `uuid` | string | System UUID |

**Implementation:** WMI `Win32_BIOS` + `Win32_BaseBoard` + `Win32_ComputerSystemProduct`

---

### `hardware.memory` 🟢 Read

Physical memory module details.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `modules` | array | Memory module objects |
| `total_slots` | integer | Total DIMM slots |
| `used_slots` | integer | Occupied slots |
| `total_capacity_gb` | float | Total installed RAM |

**Module object:**
| Field | Type | Description |
|---|---|---|
| `slot` | string | Physical slot (e.g., "DIMM_A1") |
| `capacity_gb` | float | Module capacity |
| `speed_mhz` | integer | Speed in MHz |
| `type` | string | "DDR4" / "DDR5" |
| `manufacturer` | string | Module manufacturer |
| `part_number` | string | Part number |
| `serial_number` | string | Serial number |

**Implementation:** WMI `Win32_PhysicalMemory` + `Win32_PhysicalMemoryArray`

---

### `hardware.gpu` 🟢 Read

GPU/display adapter information.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `adapters` | array | GPU objects |

**GPU object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Adapter name |
| `manufacturer` | string | Manufacturer |
| `driver_version` | string | Driver version |
| `driver_date` | string | Driver date |
| `vram_bytes` | integer | Video RAM |
| `resolution` | string | Current resolution (e.g., "1920x1080") |
| `refresh_rate_hz` | integer | Current refresh rate |
| `status` | string | "ok" / "error" |

**Implementation:** WMI `Win32_VideoController`

---

### `hardware.battery` 🟢 Read

Battery status (laptops/UPS).

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `batteries` | array | Battery objects |
| `on_ac_power` | boolean | Whether on AC power |

**Battery object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Battery name |
| `status` | string | "charging" / "discharging" / "full" / "not_charging" |
| `charge_percent` | integer | Charge percentage |
| `estimated_runtime_minutes` | integer / null | Estimated minutes remaining |
| `design_capacity_mwh` | integer | Design capacity |
| `full_charge_capacity_mwh` | integer | Current full charge capacity |
| `health_percent` | float | Battery health (full charge / design capacity) |
| `cycle_count` | integer / null | Charge cycle count |

**Implementation:** WMI `Win32_Battery` + `BatteryStatus`

---

## 13. `time.*` — Time and NTP

### `time.info` 🟢 Read

System time and timezone configuration.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `local_time` | string | ISO 8601 local time |
| `utc_time` | string | ISO 8601 UTC time |
| `timezone_id` | string | Timezone ID (e.g., "Eastern Standard Time") |
| `timezone_display` | string | Display name (e.g., "(UTC-05:00) Eastern Time") |
| `utc_offset` | string | UTC offset (e.g., "-05:00") |
| `daylight_saving` | boolean | Whether DST is in effect |
| `ntp_server` | string | Configured NTP server |
| `last_sync` | string / null | ISO 8601 last NTP sync |
| `source` | string | Time source: "ntp" / "domain" / "local" |

**Implementation:** `TimeZoneInfo.Local` + W32Time service query via `w32tm /query /status`

---

### `time.sync` 🟡 Operate

Force NTP time synchronization.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `synced` | boolean | Whether sync succeeded |
| `source` | string | NTP server used |
| `offset_ms` | float | Time offset corrected |
| `elapsed_ms` | integer | Sync operation time |

**Implementation:** `w32tm /resync /force`

---

### `time.set_timezone` 🟡 Operate

Change system timezone.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `timezone_id` | string | Timezone ID (e.g., "Eastern Standard Time", "UTC") |

**Response:**
| Field | Type | Description |
|---|---|---|
| `previous_timezone` | string | Previous timezone ID |
| `new_timezone` | string | New timezone ID |
| `local_time` | string | Current local time in new timezone |

**Implementation:** `tzutil /s "{timezone_id}"` or `SetDynamicTimeZoneInformation`

---

## 14. `registry.*` — Windows Registry

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

## 15. `iis.*` — IIS Web Server

### `iis.sites` 🟢 Read

List all IIS websites.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `sites` | array | Site objects |
| `count` | integer | Total sites |

**Site object:**
| Field | Type | Description |
|---|---|---|
| `id` | integer | Site ID |
| `name` | string | Site name |
| `state` | string | "started" / "stopped" |
| `bindings` | array | `[{protocol, host, port, ip, cert_hash}]` |
| `physical_path` | string | Root directory |
| `application_pool` | string | App pool name |
| `log_directory` | string | Log file path |
| `protocols_enabled` | array | ["http", "https", "net.tcp", ...] |
| `applications` | array | `[{path, physical_path, pool}]` |
| `virtual_directories` | array | `[{path, physical_path}]` |

**Implementation:** `Microsoft.Web.Administration.ServerManager.Sites`

---

### `iis.pools` 🟢 Read

List application pools.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `pools` | array | Pool objects |
| `count` | integer | Total pools |

**Pool object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Pool name |
| `state` | string | "started" / "stopped" |
| `runtime_version` | string | CLR version (e.g., "v4.0", "No Managed Code") |
| `pipeline_mode` | string | "integrated" / "classic" |
| `identity` | string | "ApplicationPoolIdentity" / "LocalSystem" / "NetworkService" / custom |
| `worker_processes` | integer | Active worker process count |
| `max_processes` | integer | Maximum worker processes |
| `auto_start` | boolean | Auto-start enabled |
| `cpu_limit` | integer | CPU limit percentage (0 = unlimited) |
| `memory_limit_kb` | integer | Private memory limit (0 = unlimited) |
| `idle_timeout_minutes` | integer | Idle timeout |
| `recycle_interval_minutes` | integer | Regular recycle interval |
| `recycle_times` | array | Scheduled recycle times |
| `enable_32bit` | boolean | 32-bit application mode |
| `start_mode` | string | "ondemand" / "alwaysrunning" |

**Implementation:** `ServerManager.ApplicationPools`

---

### `iis.site.start` 🟡 Operate

Start a website.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Site name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Site name |
| `state` | string | New state |
| `started` | boolean | Success |

**Implementation:** `ServerManager.Sites[name].Start()`

---

### `iis.site.stop` 🟡 Operate

Stop a website.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Site name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Site name |
| `state` | string | New state |
| `stopped` | boolean | Success |

**Implementation:** `ServerManager.Sites[name].Stop()`

---

### `iis.pool.start` 🟡 Operate

Start an application pool.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Pool name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Pool name |
| `state` | string | New state |

**Implementation:** `ServerManager.ApplicationPools[name].Start()`

---

### `iis.pool.stop` 🟡 Operate

Stop an application pool.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Pool name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Pool name |
| `state` | string | New state |

**Implementation:** `ServerManager.ApplicationPools[name].Stop()`

---

### `iis.pool.recycle` 🟡 Operate

Recycle an application pool (graceful restart of worker processes).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Pool name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Pool name |
| `recycled` | boolean | Success |
| `previous_pid` | integer / null | Previous worker PID |

**Implementation:** `ServerManager.ApplicationPools[name].Recycle()`

---

### `iis.site.config` 🟢 Read

Get detailed site configuration.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Site name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Site name |
| `bindings` | array | Full binding details with cert info |
| `limits` | object | `{max_bandwidth, max_connections, connection_timeout_seconds}` |
| `logging` | object | `{enabled, directory, format, fields, rollover, max_size_bytes}` |
| `compression` | object | `{static_enabled, dynamic_enabled}` |
| `default_documents` | array | Default document list |
| `error_pages` | array | Custom error pages `[{status_code, path, type}]` |
| `authentication` | object | `{anonymous, windows, basic, digest}` enabled status |
| `ssl_settings` | object | `{require_ssl, client_certs}` |

**Implementation:** `ServerManager.Sites[name]` + `GetWebConfiguration()`

---

### `iis.site.config.set` 🟡 Operate

Update site configuration.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Site name |
| `setting` | string | Setting to change: "binding_add" / "binding_remove" / "physical_path" / "default_documents" / "authentication" |
| `value` | object | Setting-specific value object |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Site name |
| `setting` | string | Setting changed |
| `applied` | boolean | Success |
| `details` | object | Change details |

**Implementation:** `ServerManager` + `CommitChanges()`

---

### `iis.pool.config` 🟢 Read

Get detailed app pool configuration.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Pool name |

**Response:** All fields from pool object in `iis.pools` plus:
| Field | Type | Description |
|---|---|---|
| `recycling` | object | `{regular_interval, specific_times, memory_limit_kb, virtual_memory_limit_kb, request_limit}` |
| `rapid_fail_protection` | object | `{enabled, max_failures, interval_minutes, action}` |
| `process_model` | object | `{identity_type, username, idle_action, shutdown_time_limit_seconds, startup_time_limit_seconds, ping_enabled, ping_interval_seconds}` |

**Implementation:** `ServerManager.ApplicationPools[name]` full property access

---

### `iis.pool.config.set` 🟡 Operate

Update app pool configuration.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Pool name |
| `setting` | string | Setting: "runtime_version" / "pipeline_mode" / "identity" / "recycle_interval" / "memory_limit" / "idle_timeout" / "start_mode" |
| `value` | string / integer | New value |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Pool name |
| `setting` | string | Setting changed |
| `previous_value` | any | Previous value |
| `new_value` | any | New value |

**Implementation:** `ServerManager.ApplicationPools[name]` + `CommitChanges()`

---

### `iis.worker_processes` 🟢 Read

List active IIS worker processes.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `workers` | array | Worker objects |

**Worker object:**
| Field | Type | Description |
|---|---|---|
| `pid` | integer | Process ID |
| `pool_name` | string | Application pool |
| `state` | string | "running" / "starting" / "stopping" |
| `cpu_percent` | float | CPU usage |
| `memory_mb` | float | Memory usage |
| `active_requests` | integer | Current active requests |

**Implementation:** `ServerManager.WorkerProcesses`

---

## 16. `ad.*` — Active Directory

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

### `ad.groups` 🔵 Domain

List or search AD groups.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter` | string (optional) | Search by name (contains) |
| `type` | string (optional) | "security" / "distribution" / "all". Default: "all" |
| `scope` | string (optional) | "domainlocal" / "global" / "universal" / "all". Default: "all" |
| `ou` | string (optional) | Specific OU |
| `limit` | integer (optional) | Max results. Default: 100 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `groups` | array | Group objects |
| `returned_count` | integer | Results returned |

**Group object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Group name (CN) |
| `sam_account_name` | string | SAM name |
| `distinguished_name` | string | Full DN |
| `description` | string / null | Description |
| `group_type` | string | "security" / "distribution" |
| `group_scope` | string | "domainlocal" / "global" / "universal" |
| `member_count` | integer | Direct member count |
| `managed_by` | string / null | Manager DN |
| `email` | string / null | Group email |
| `created` | string | ISO 8601 |

**Implementation:** LDAP query `(objectClass=group)` with `groupType` bitmask filter

---

### `ad.user.info` 🔵 Domain

Detailed AD user attributes.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `identity` | string | SAM account name, UPN, or DN |

**Response:** All fields from `ad.users` user object plus:
| Field | Type | Description |
|---|---|---|
| `account_expires` | string / null | ISO 8601 account expiration |
| `logon_count` | integer | Total logon count |
| `bad_password_count` | integer | Failed password attempts |
| `lockout_time` | string / null | ISO 8601 lockout time |
| `home_directory` | string / null | Home directory |
| `home_drive` | string / null | Home drive letter |
| `profile_path` | string / null | Profile path |
| `logon_script` | string / null | Logon script |
| `allowed_workstations` | array | Allowed logon workstations |
| `last_bad_password` | string / null | ISO 8601 last bad password attempt |
| `user_account_control` | integer | UAC flags (raw) |
| `uac_flags` | array | Decoded UAC flags (e.g., "NORMAL_ACCOUNT", "DONT_EXPIRE_PASSWD") |
| `sid` | string | Security Identifier |
| `object_guid` | string | Object GUID |
| `all_groups` | array | All group memberships (recursive, including nested) |

**Implementation:** LDAP query by identity → fetch all attributes + recursive `memberOf` resolution

---

### `ad.user.groups` 🔵 Domain

All groups a user belongs to (recursive).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `identity` | string | SAM, UPN, or DN |
| `recursive` | boolean (optional) | Include nested group memberships. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `user` | string | User identity |
| `groups` | array | `[{name, dn, type, scope, direct}]` — `direct` = true if direct member |
| `count` | integer | Total groups |

**Implementation:** LDAP `memberOf` attribute + recursive DN resolution, or `tokenGroups` attribute for recursive SID list

---

### `ad.group.members` 🔵 Domain

Members of an AD group.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `identity` | string | Group name, SAM, or DN |
| `recursive` | boolean (optional) | Include nested group members. Default: false |
| `limit` | integer (optional) | Max results. Default: 500 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `group` | string | Group name |
| `members` | array | `[{name, sam_account_name, type, dn, enabled}]` — type = "user" / "group" / "computer" |
| `count` | integer | Total members |

**Implementation:** LDAP `member` attribute enumeration + optional recursion

---

### `ad.computers` 🔵 Domain

List domain computers.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `filter` | string (optional) | Filter by name |
| `ou` | string (optional) | Specific OU |
| `os_filter` | string (optional) | Filter by OS (e.g., "Server 2022") |
| `stale_days` | integer (optional) | Only show computers not logged on in N days |
| `limit` | integer (optional) | Max results. Default: 100 |

**Response:**
| Field | Type | Description |
|---|---|---|
| `computers` | array | Computer objects |

**Computer object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Computer name |
| `dns_hostname` | string | FQDN |
| `operating_system` | string | OS name |
| `os_version` | string | OS version |
| `enabled` | boolean | Account enabled |
| `last_logon` | string / null | ISO 8601 |
| `created` | string | ISO 8601 |
| `ou` | string | Parent OU |
| `description` | string / null | Description |
| `ipv4_address` | string / null | Last known IP |

**Implementation:** LDAP query `(objectClass=computer)`

---

### `ad.ou.list` 🔵 Domain

List organizational units.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `root` | string (optional) | Starting DN. Default: domain root |
| `depth` | integer (optional) | Max depth. Default: unlimited |

**Response:**
| Field | Type | Description |
|---|---|---|
| `ous` | array | OU objects |

**OU object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | OU name |
| `distinguished_name` | string | Full DN |
| `description` | string / null | Description |
| `protected` | boolean | Protected from accidental deletion |
| `child_ou_count` | integer | Child OUs |
| `user_count` | integer | Direct user count |
| `computer_count` | integer | Direct computer count |
| `group_count` | integer | Direct group count |
| `gpo_links` | array | Linked GPO names |

**Implementation:** LDAP query `(objectClass=organizationalUnit)` + count queries per OU

---

### `ad.user.enable` 🔵 Domain

Enable a disabled user account.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `identity` | string | SAM, UPN, or DN |

**Response:**
| Field | Type | Description |
|---|---|---|
| `identity` | string | User identity |
| `previously_enabled` | boolean | Was already enabled |
| `enabled` | boolean | Current state (true) |

**Implementation:** Clear `ACCOUNTDISABLE` flag (0x0002) in `userAccountControl`

---

### `ad.user.disable` 🔵 Domain

Disable a user account.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `identity` | string | SAM, UPN, or DN |

**Response:**
| Field | Type | Description |
|---|---|---|
| `identity` | string | User identity |
| `previously_enabled` | boolean | Was enabled before |
| `enabled` | boolean | Current state (false) |

**Implementation:** Set `ACCOUNTDISABLE` flag in `userAccountControl`

---

### `ad.user.unlock` 🔵 Domain

Unlock a locked-out account.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `identity` | string | SAM, UPN, or DN |

**Response:**
| Field | Type | Description |
|---|---|---|
| `identity` | string | User identity |
| `was_locked` | boolean | Whether account was locked |
| `unlocked` | boolean | Success |

**Implementation:** Set `lockoutTime = 0` via LDAP modify

---

### `ad.user.resetpw` 🔴 Dangerous

Reset a user's password.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `identity` | string | SAM, UPN, or DN |
| `new_password` | string | New password |
| `must_change` | boolean (optional) | Force change at next logon. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `identity` | string | User identity |
| `reset` | boolean | Success |
| `must_change_at_logon` | boolean | Whether flag was set |

**Security:** Password is never logged in audit trail. Only the fact that a reset occurred is recorded.

**Implementation:** `DirectoryEntry.Invoke("SetPassword", newPassword)` + set `pwdLastSet = 0` if must_change

---

## 17. `hyperv.*` — Hyper-V Virtual Machines

### `hyperv.vms` 🔵 Domain

List Hyper-V virtual machines.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `state` | string (optional) | "running" / "off" / "saved" / "paused" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `vms` | array | VM objects |
| `count` | integer | Total count |

**VM object:**
| Field | Type | Description |
|---|---|---|
| `id` | string | VM GUID |
| `name` | string | VM name |
| `state` | string | "running" / "off" / "saved" / "paused" / "starting" / "saving" / "stopping" / "reset" |
| `generation` | integer | 1 or 2 |
| `version` | string | Configuration version |
| `cpu_count` | integer | Virtual processor count |
| `memory_assigned_mb` | integer | Currently assigned RAM |
| `memory_startup_mb` | integer | Startup RAM |
| `memory_dynamic` | boolean | Dynamic memory enabled |
| `uptime` | string | Uptime (human-readable) |
| `status` | string | "Operating normally" / "Critical" / etc. |
| `integration_services_version` | string / null | IC version |
| `checkpoint_count` | integer | Number of checkpoints |
| `path` | string | Configuration path |
| `notes` | string | VM notes |

**Implementation:** WMI `Msvm_ComputerSystem` (root/virtualization/v2)

---

### `hyperv.vm.info` 🔵 Domain

Detailed VM information.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |

**Response:** All fields from VM object plus:
| Field | Type | Description |
|---|---|---|
| `disks` | array | `[{controller, path, size_gb, format, type}]` — VHD/VHDX details |
| `network_adapters` | array | `[{name, switch_name, mac_address, vlan_id, ip_addresses}]` |
| `dvd_drives` | array | `[{controller, path}]` |
| `snapshots` | array | `[{name, created, parent}]` — checkpoint tree |
| `replication_state` | string / null | Replication status if configured |
| `memory_demand_mb` | integer / null | Current memory demand (if running) |
| `cpu_usage_percent` | float / null | Current CPU usage (if running) |
| `heartbeat` | string / null | Guest heartbeat status |
| `guest_os` | string / null | Detected guest OS |
| `secure_boot` | boolean | Secure Boot enabled (Gen 2) |
| `tpm` | boolean | vTPM enabled |
| `automatic_start_action` | string | "nothing" / "start_if_running" / "always_start" |
| `automatic_stop_action` | string | "save" / "turn_off" / "shutdown" |

**Implementation:** WMI `Msvm_*` associated objects (storage, network, memory, processor settings)

---

### `hyperv.vm.start` 🔵 Domain

Start a VM.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | VM name |
| `state` | string | New state |
| `started` | boolean | Success |

**Implementation:** `Msvm_ComputerSystem.RequestStateChange(2)` — 2 = Enabled/Running

---

### `hyperv.vm.stop` 🔵 Domain

Stop a VM.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |
| `force` | boolean (optional) | Turn off (vs. graceful shutdown). Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | VM name |
| `state` | string | New state |
| `method` | string | "shutdown" / "turn_off" |
| `stopped` | boolean | Success |

**Implementation:** Graceful: `Msvm_ShutdownComponent.InitiateShutdown()`. Force: `RequestStateChange(3)` — 3 = Disabled/Off

---

### `hyperv.vm.restart` 🔵 Domain

Restart a VM.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |
| `force` | boolean (optional) | Hard reset (vs. graceful restart). Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | VM name |
| `method` | string | "restart" / "reset" |
| `state` | string | Current state |

**Implementation:** Graceful: shutdown + start. Force: `RequestStateChange(10)` — 10 = Reset

---

### `hyperv.vm.snapshot` 🔵 Domain

Create a checkpoint/snapshot.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |
| `name` | string (optional) | Checkpoint name. Default: auto-generated with timestamp |

**Response:**
| Field | Type | Description |
|---|---|---|
| `vm_name` | string | VM name |
| `snapshot_name` | string | Checkpoint name |
| `created` | boolean | Success |
| `snapshot_id` | string | Snapshot GUID |

**Implementation:** `Msvm_VirtualSystemSnapshotService.CreateSnapshot()`

---

### `hyperv.switches` 🔵 Domain

List virtual switches.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `switches` | array | Switch objects |

**Switch object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Switch name |
| `id` | string | Switch GUID |
| `type` | string | "external" / "internal" / "private" |
| `interface` | string / null | Bound physical NIC (external only) |
| `management_os` | boolean | Allow host OS access |
| `connected_vms` | integer | VMs connected |

**Implementation:** WMI `Msvm_VirtualEthernetSwitch`

---

## 18. `gpo.*` — Group Policy

### `gpo.list` 🔵 Domain

List Group Policy Objects applied to this machine.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `target` | string (optional) | "computer" / "user" / "both". Default: "both" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `computer_gpos` | array | GPOs applied to computer |
| `user_gpos` | array | GPOs applied to current user |

**GPO object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | GPO display name |
| `guid` | string | GPO GUID |
| `status` | string | "applied" / "denied" / "not_applied" |
| `link_location` | string | OU/domain where linked |
| `link_order` | integer | Link order (priority) |
| `enabled` | boolean | Link enabled |
| `enforced` | boolean | Link enforced (no override) |
| `wmi_filter` | string / null | WMI filter name |
| `last_applied` | string / null | ISO 8601 |

**Implementation:** `gpresult /r` parsed or WMI `RSOP_GPO` (root/rsop/computer and root/rsop/user)

---

### `gpo.result` 🔵 Domain

Resultant Set of Policy — detailed policy results.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `target` | string (optional) | "computer" / "user" / "both". Default: "both" |
| `format` | string (optional) | "summary" / "full". Default: "summary" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `computer_settings` | object | Applied computer policies grouped by category |
| `user_settings` | object | Applied user policies grouped by category |
| `security_groups` | array | Effective security group memberships |
| `wmi_filters` | array | Evaluated WMI filters and results |
| `errors` | array | GPO processing errors |

Categories include: Security Settings, Administrative Templates, Software Installation, Scripts, Folder Redirection, etc.

**Implementation:** `gpresult /h` → parse HTML report, or WMI RSOP namespace queries

---

### `gpo.update` 🟡 Operate

Force Group Policy refresh.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `target` | string (optional) | "computer" / "user" / "both". Default: "both" |
| `force` | boolean (optional) | Reapply all policies, not just changed. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `computer_updated` | boolean | Computer policy refreshed |
| `user_updated` | boolean | User policy refreshed |
| `force` | boolean | Whether force was applied |

**Implementation:** `gpupdate /force /target:{target}`

---

## 19. `printer.*` — Print Management

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

### `printer.info` 🟢 Read

Detailed printer information.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:** All fields from printer object plus:
| Field | Type | Description |
|---|---|---|
| `server_name` | string / null | Print server (null for local) |
| `driver_version` | string | Driver version |
| `print_processor` | string | Print processor name |
| `data_type` | string | Default data type (RAW, EMF) |
| `priority` | integer | Printer priority (1-99) |
| `published` | boolean | Published in AD |
| `paper_sizes` | array | Supported paper sizes |
| `resolutions` | array | Supported DPI values |
| `queued_bytes` | integer | Total bytes in queue |
| `total_pages_printed` | integer | Lifetime pages printed |
| `total_jobs_printed` | integer | Lifetime jobs printed |
| `average_pages_per_minute` | float / null | Rated speed |
| `last_error` | string / null | Last error description |
| `last_error_time` | string / null | ISO 8601 |

**Implementation:** WMI `Win32_Printer` + `Win32_PrinterConfiguration`

---

### `printer.jobs` 🟢 Read

List print jobs in a printer's queue.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string (optional) | Printer name. If omitted, all printers. |
| `status` | string (optional) | "pending" / "printing" / "error" / "paused" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `jobs` | array | Job objects |
| `total_count` | integer | Total jobs |

**Job object:**
| Field | Type | Description |
|---|---|---|
| `job_id` | integer | Job ID |
| `printer` | string | Printer name |
| `document` | string | Document name |
| `owner` | string | User who submitted |
| `status` | string | "spooling" / "printing" / "paused" / "error" / "deleting" / "offline" / "retained" / "complete" |
| `status_code` | integer | Raw status flags |
| `priority` | integer | Job priority (1-99) |
| `position` | integer | Position in queue |
| `submitted` | string | ISO 8601 submission time |
| `start_time` | string / null | ISO 8601 print start |
| `pages_printed` | integer | Pages printed so far |
| `total_pages` | integer | Total pages |
| `size_bytes` | integer | Job size |
| `data_type` | string | "RAW" / "EMF" |
| `color` | boolean | Color job |

**Implementation:** WMI `Win32_PrintJob`

---

### `printer.job.cancel` 🟡 Operate

Cancel a print job.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |
| `document` | string | Document name |
| `cancelled` | boolean | Success |

**Implementation:** WMI `Win32_PrintJob.Delete()`

---

### `printer.job.pause` 🟡 Operate

Pause a print job.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |
| `paused` | boolean | Success |

**Implementation:** WMI `Win32_PrintJob.Pause()`

---

### `printer.job.resume` 🟡 Operate

Resume a paused print job.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |
| `resumed` | boolean | Success |

**Implementation:** WMI `Win32_PrintJob.Resume()`

---

### `printer.queue.clear` 🟡 Operate

Cancel all jobs in a printer's queue.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `jobs_cancelled` | integer | Number of jobs cancelled |

**Implementation:** WMI enumerate `Win32_PrintJob` where `Name LIKE '{printer}%'` → `Delete()` each

---

### `printer.spooler.status` 🟢 Read

Check Print Spooler service status.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `status` | string | "running" / "stopped" / "paused" |
| `pid` | integer / null | Process ID |
| `startup_type` | string | Service startup type |
| `spool_directory` | string | Spool directory path |
| `spool_size_mb` | float | Current spool directory size |
| `temp_files_count` | integer | Number of spool temp files |

**Implementation:** `ServiceController("Spooler")` + directory enumeration of `%SystemRoot%\System32\spool\PRINTERS`

---

### `printer.spooler.restart` 🟡 Operate

Restart the Print Spooler service (common fix for stuck jobs).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `clear_queue` | boolean (optional) | Delete all spool files before restarting. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `restarted` | boolean | Success |
| `spool_files_cleared` | integer | Spool files deleted (if clear_queue=true) |
| `elapsed_ms` | integer | Restart time |

**Implementation:** Stop Spooler → optionally delete files in spool directory → Start Spooler

---

### `printer.spooler.clear` 🟡 Operate

Clear the spool directory without restarting (nuclear option for stuck spooler).

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `files_deleted` | integer | Spool files removed |
| `bytes_freed` | integer | Bytes freed |
| `spooler_restarted` | boolean | Whether spooler was restarted after clearing |

**Implementation:** Stop Spooler → delete `*.SHD` and `*.SPL` from spool directory → Start Spooler

---

### `printer.pause` 🟡 Operate

Pause a printer (holds all new jobs).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `paused` | boolean | Success |
| `pending_jobs` | integer | Jobs currently in queue |

**Implementation:** WMI `Win32_Printer.Pause()`

---

### `printer.resume` 🟡 Operate

Resume a paused printer.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `resumed` | boolean | Success |
| `pending_jobs` | integer | Jobs that will now process |

**Implementation:** WMI `Win32_Printer.Resume()`

---

### `printer.test` 🟡 Operate

Print a test page.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `test_page_sent` | boolean | Success |
| `job_id` | integer | Test page job ID |

**Implementation:** WMI `Win32_Printer.PrintTestPage()`

---

### `printer.set_default` 🟡 Operate

Set default printer.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | New default printer |
| `previous_default` | string / null | Previous default |

**Implementation:** WMI `Win32_Printer.SetDefaultPrinter()`

---

### `printer.drivers` 🟢 Read

List installed printer drivers.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `drivers` | array | Driver objects |
| `count` | integer | Total count |

**Driver object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Driver name |
| `version` | string | Version |
| `manufacturer` | string / null | Manufacturer |
| `architecture` | string | "x64" / "x86" |
| `monitor_name` | string / null | Port monitor |
| `used_by` | array | Printers using this driver |

**Implementation:** WMI `Win32_PrinterDriver`

---

### `printer.ports` 🟢 Read

List printer ports.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `ports` | array | Port objects |

**Port object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Port name |
| `type` | string | "tcp" / "usb" / "lpt" / "local" / "wsd" |
| `address` | string / null | IP address (for TCP ports) |
| `queue` | string / null | Remote queue name |
| `protocol` | string / null | "raw" / "lpr" (for TCP ports) |
| `port_number` | integer / null | TCP port (default 9100 for RAW, 515 for LPR) |
| `used_by` | array | Printers using this port |

**Implementation:** WMI `Win32_TCPIPPrinterPort` + `Win32_Port`

---

## Summary

| Domain        | Tools   | Tier Range | Primary Implementation          |
| ------------- | ------- | ---------- | ------------------------------- |
| `system.*`    | 6       | 🟢-🔴      | Environment + WMI               |
| `process.*`   | 6       | 🟢-🟡      | System.Diagnostics + WMI        |
| `service.*`   | 8       | 🟢-🟡      | ServiceController + WMI         |
| `log.*`       | 5       | 🟢-🟡      | EventLog APIs                   |
| `network.*`   | 10      | 🟢-🟡      | NetworkInterface + COM Firewall |
| `file.*`      | 13      | 🟢-🟡      | System.IO + FileSecurity + WMI  |
| `identity.*`  | 5       | 🟢-🔴      | WMI + DirectoryEntry            |
| `storage.*`   | 5       | 🟢         | DriveInfo + WMI                 |
| `schedule.*`  | 6       | 🟢-🟡      | COM ITaskService                |
| `security.*`  | 5       | 🟢         | X509Store + AuditPol + Defender |
| `container.*` | 8       | 🟢-🟡      | Docker named pipe API           |
| `hardware.*`  | 6       | 🟢         | WMI Win32\_\*                   |
| `time.*`      | 3       | 🟢-🟡      | TimeZoneInfo + W32Time          |
| `registry.*`  | 6       | 🟢-🔴      | Microsoft.Win32.Registry        |
| `iis.*`       | 11      | 🟢-🟡      | Microsoft.Web.Administration    |
| `ad.*`        | 11      | 🔵-🔴      | DirectoryServices.Protocols     |
| `hyperv.*`    | 6       | 🔵         | WMI Msvm\_\*                    |
| `gpo.*`       | 3       | 🔵-🟡      | gpresult + WMI RSOP             |
| `printer.*`   | 16      | 🟢-🟡      | WMI Win32_Printer/PrintJob      |
| **Total**     | **139** |            |                                 |
