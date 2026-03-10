# mcpw — Test Specification (TDD)

Comprehensive test list for all mcpw tools, organized for Test-Driven Development. Write these tests **before** implementing each tool.

**Conventions:**

- ✅ = must pass for tool to be considered complete
- 🔒 = security test (critical)
- ⚡ = edge case
- 🧪 = integration test (requires real OS interaction)
- 🎭 = mock test (uses mocked Windows APIs)

Each tool has tests grouped by category: **Input Validation**, **Happy Path**, **Error Handling**, **Edge Cases**, **Security**, and **Privilege**.

---

## Global Tests (Apply to All Tools)

These tests apply to every single tool in mcpw and should be run as a shared test suite.

### MCP Protocol Compliance

- ✅ Returns valid JSON-RPC 2.0 response for every tool call
- ✅ Returns `error` object with `code` and `message` for invalid requests
- ✅ Returns `error` with code `-32601` (Method Not Found) for disabled domains
- ✅ Returns `error` with code `-32602` (Invalid Params) for missing required parameters
- ✅ Returns `error` with code `-32602` for wrong parameter types (string where integer expected, etc.)
- ✅ Returns `error` with code `-32603` (Internal Error) for unexpected failures, with no stack trace leaked
- ✅ Tool name in request matches exactly (case-sensitive)
- ✅ Extra/unknown parameters are ignored (not rejected)
- ✅ Response includes all documented required fields
- ✅ Response field types match documentation (string is string, not integer, etc.)
- ✅ Null/optional fields are present as `null`, not omitted
- ✅ Timestamps are always ISO 8601 UTC format
- ✅ Tool manifest (`tools/list`) includes all enabled tools with correct names, descriptions, and input schemas

### Configuration

- ✅ Tool in `enabledDomains` is accessible
- ✅ Tool in `disabledDomains` returns ToolNotFound
- ✅ Tool not in either list returns ToolNotFound
- ✅ Config file missing → starts with safe defaults
- ✅ Config file malformed JSON → fails with clear error, does not start
- ✅ Config reload does not interrupt in-flight tool calls

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

## 1. `system.*`

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

## 2. `process.*`

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

## 3. `service.*`

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

## 4. `log.*`

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

### `log.search`

**Happy Path:**

- ✅ `query: "error"` finds entries containing "error" in message
- ✅ `source: "Service Control Manager"` returns only SCM entries
- ✅ `event_id: 7036` returns only events with that ID
- ✅ `since` and `until` filters by time range correctly
- ✅ `level: "error"` combined with `query` applies both filters
- ✅ `limit` caps results

**Input Validation:**

- ✅ `since` after `until` → error
- ✅ `since` in future → empty results
- ✅ `limit` < 0 → error

**Error Handling:**

- ✅ No results match → empty array, `returned_count: 0`
- ✅ Invalid XPath generated internally → error (not crash)

**Edge Cases:**

- ⚡ `query` with special XPath characters (quotes, brackets) → properly escaped
- ⚡ `query` with Unicode characters
- ⚡ Very broad search on large log → respects limit, returns within timeout
- ⚡ `event_id: 0` → valid (some events use ID 0)

---

### `log.stream`

**Happy Path:**

- 🧪 Starts subscription → receives new events as they're written
- 🧪 `level: "error"` only emits error-level events
- 🧪 `source` filter works in streaming mode

**Error Handling:**

- ✅ Invalid channel → error on subscribe
- ✅ Channel disabled during stream → stream terminates gracefully

**Edge Cases:**

- ⚡ High-volume log channel (many events/second) → does not drop events or OOM
- ⚡ Long-running stream (hours) → does not leak memory or handles

---

### `log.channels`

**Happy Path:**

- ✅ Returns non-empty list
- ✅ Contains "Application", "System", "Security"
- ✅ `include_empty: false` excludes channels with zero records
- ✅ `filter: "Microsoft"` returns only Microsoft channels
- ✅ Each channel has `name`, `enabled`, `record_count`

**Edge Cases:**

- ⚡ System with hundreds of channels (operational/analytic logs)
- ⚡ `filter` matches no channels → empty array

---

### `log.clear`

**Happy Path:**

- 🎭 Clears a log channel → `records_cleared` > 0
- 🎭 `backup_path` saves .evtx backup before clearing
- 🎭 Channel is empty after clearing

**Input Validation:**

- ✅ Empty channel name → error
- ✅ `backup_path` in blocked directory → error

**Security:**

- 🔒 Requires Operate privilege tier
- 🔒 Clear action is logged in audit trail
- 🔒 Cannot clear Security log without explicit policy

**Error Handling:**

- ✅ Already empty channel → `records_cleared: 0`
- ✅ Channel access denied → error
- ✅ Backup path not writable → error, channel NOT cleared

**Edge Cases:**

- ⚡ Clearing channel while events are being written → new events after clear are not lost

---

## 5. `network.*`

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

### `network.ports`

**Happy Path:**

- ✅ Returns list of listening ports
- ✅ Contains common ports (135 RPC, 445 SMB on domain machine)
- ✅ `protocol: "tcp"` returns only TCP listeners
- ✅ `protocol: "udp"` returns only UDP listeners
- ✅ Each listener has `pid` > 0 and valid `process_name`
- ✅ `local_port` is valid port number (1-65535)

**Edge Cases:**

- ⚡ Port bound to 0.0.0.0 vs 127.0.0.1 vs specific IP
- ⚡ Same port on different IPs (multiple listeners)
- ⚡ High port number (>49152) for ephemeral range
- ⚡ IPv6 listeners ([::]:80)

---

### `network.connections`

**Happy Path:**

- ✅ Returns active connections
- ✅ `state: "established"` shows only ESTABLISHED
- ✅ `pid` filter returns only connections for that process
- ✅ `port` filter matches local or remote port
- ✅ Each connection has valid state string

**Edge Cases:**

- ⚡ `state: "time_wait"` may have thousands of entries → respects limit
- ⚡ Connection state changes during enumeration
- ⚡ UDP "connections" (stateless) have limited info
- ⚡ `limit: 1` returns 1 connection

---

### `network.dns`

**Happy Path (config):**

- ✅ Returns `hostname` matching system hostname
- ✅ Returns `dns_servers` per interface
- ✅ Returns `search_suffixes` if configured

**Happy Path (resolve):**

- ✅ `resolve: "localhost"` returns 127.0.0.1
- ✅ `resolve: "127.0.0.1", type: "PTR"` returns localhost
- ✅ Resolving public domain returns valid IP
- ✅ `elapsed_ms` > 0

**Error Handling:**

- ✅ `resolve: "nonexistent.invalid"` → error "Name not resolved"
- ✅ DNS server unreachable → timeout error

**Security:**

- 🔒 Cannot resolve internal hostnames that would leak network topology (configurable)

**Edge Cases:**

- ⚡ CNAME chain resolution
- ⚡ Domain with many A records
- ⚡ IPv6 AAAA resolution
- ⚡ Very slow DNS response (near timeout)

---

### `network.firewall`

**Happy Path:**

- ✅ Returns `firewall_enabled` per profile
- ✅ Returns non-empty rules array (Windows has default rules)
- ✅ `direction: "in"` only returns inbound rules
- ✅ `enabled_only: true` excludes disabled rules
- ✅ `profile: "domain"` filters by profile
- ✅ Each rule has all required fields

**Edge Cases:**

- ⚡ Rule with "any" for all fields
- ⚡ Rule with IP range notation
- ⚡ Rule with port range (e.g., "8000-9000")
- ⚡ Rule tied to a specific service
- ⚡ Thousands of rules → returns within reasonable time

---

### `network.firewall.add`

**Happy Path:**

- 🎭 Creates inbound allow rule → verifiable in `network.firewall`
- 🎭 Creates outbound block rule
- 🎭 Rule with specific port, program, and profile

**Input Validation:**

- ✅ Missing `name` → error
- ✅ Missing `direction` → error
- ✅ Invalid `protocol` → error
- ✅ Invalid port range (e.g., "99999") → error
- ✅ Duplicate rule name → error

**Security:**

- 🔒 Requires Operate privilege tier
- 🔒 Cannot create rule that opens all ports inbound
- 🔒 Rule creation logged in audit trail

---

### `network.firewall.remove`

**Happy Path:**

- 🎭 Removes existing rule → no longer appears in list

**Error Handling:**

- ✅ Non-existent rule name → error
- ✅ Protected system rule → error

---

### `network.routing`

**Happy Path:**

- ✅ Returns non-empty route table
- ✅ Contains default route (0.0.0.0/0)
- ✅ Contains loopback route (127.0.0.0/8)
- ✅ Each route has valid `metric` > 0

---

### `network.ping`

**Happy Path:**

- ✅ Pinging 127.0.0.1 → all succeed, low latency
- ✅ `count: 1` sends exactly 1 ping
- ✅ Returns correct `sent`, `received`, `lost` counts
- ✅ `loss_percent` = 0 for successful pings
- ✅ `avg_ms`, `min_ms`, `max_ms` are calculated correctly
- ✅ `resolved_address` is an IP address

**Error Handling:**

- ✅ Unreachable host → `loss_percent: 100`
- ✅ Unresolvable hostname → error
- ✅ Timeout on all pings → all results show timeout

**Edge Cases:**

- ⚡ `count: 100` → completes in reasonable time
- ⚡ Ping to IPv6 address
- ⚡ Host that drops some packets → partial loss

---

### `network.traceroute`

**Happy Path:**

- ✅ Trace to 127.0.0.1 → 1 hop
- ✅ Returns `hops` array with incrementing hop numbers
- ✅ `reached: true` when target is reached
- ✅ Each hop has `address` (or "\*" for timeout)

**Error Handling:**

- ✅ Unresolvable target → error
- ✅ Unreachable target → `reached: false`, partial hops

**Edge Cases:**

- ⚡ Hop that doesn't respond (shown as \*)
- ⚡ `max_hops: 1` → single hop result
- ⚡ Asymmetric routing (different path each time)

---

## 6. `file.*`

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

### `file.share` / `file.share.create` / `file.share.remove`

**Happy Path:**

- ✅ `file.share` lists existing shares (at least IPC$, ADMIN$)
- 🎭 `file.share.create` creates a new share
- 🎭 `file.share.remove` removes a share

**Security:**

- 🔒 Cannot create share pointing outside allowed paths
- 🔒 Share creation requires Operate tier

---

## 7. `identity.*`

### `identity.users`

**Happy Path:**

- ✅ Returns non-empty list (at least Administrator and Guest)
- ✅ `include_disabled: false` excludes disabled accounts
- ✅ `filter_name` filters correctly (case-insensitive)
- ✅ Each user has `name`, `sid`, `enabled`, `groups`
- ✅ `last_logon` is valid timestamp for recently used accounts

**Edge Cases:**

- ⚡ System with many local users (>100)
- ⚡ User with empty full_name
- ⚡ Built-in accounts (DefaultAccount, WDAGUtilityAccount)

---

### `identity.groups`

**Happy Path:**

- ✅ Returns Administrators, Users, Guests groups
- ✅ Each group has `members` array
- ✅ Administrators group contains Administrator user
- ✅ `member_count` matches `members.length`

---

### `identity.whoami`

**Happy Path:**

- ✅ Returns current username
- ✅ `is_admin` is accurate
- ✅ `groups` contains group SIDs
- ✅ `privileges` lists assigned privileges
- ✅ Running as SYSTEM → `is_system: true`
- ✅ `integrity_level` matches actual level

**Edge Cases:**

- ⚡ Running as LOCAL SERVICE → specific fields populated
- ⚡ Running with UAC elevation → `elevation_type: "full"`
- ⚡ Running non-elevated → `elevation_type: "limited"`

---

### `identity.user.create`

**Happy Path:**

- 🎭 Creates user with specified name and password
- 🎭 User appears in `identity.users` list
- 🎭 `groups` parameter adds user to specified groups
- 🎭 `must_change_password` flag is set correctly

**Input Validation:**

- ✅ Empty username → error
- ✅ Username with invalid characters (/ \ [ ] : ; | = , + \* ? < >) → error
- ✅ Username > 20 characters → error
- ✅ Password doesn't meet complexity → error
- ✅ Duplicate username → error

**Security:**

- 🔒 Requires Operate tier
- 🔒 Password never appears in audit log

---

### `identity.user.delete`

**Security:**

- 🔒 Requires Dangerous tier
- 🔒 Cannot delete Administrator account
- 🔒 Cannot delete currently logged-on user
- 🔒 Cannot delete account running mcpw

---

## 8. `storage.*`

### `storage.disks`

**Happy Path:**

- ✅ Returns at least one disk (system disk)
- ✅ `size_bytes` > 0
- ✅ `status: "OK"` for healthy disk
- ✅ `partitions` count > 0 for system disk

---

### `storage.volumes`

**Happy Path:**

- ✅ Returns C: volume
- ✅ `used_bytes` + `free_bytes` ≈ `size_bytes` (within 1%)
- ✅ `file_system` is "NTFS" or "ReFS" for system volume
- ✅ `used_percent` between 0 and 100

**Edge Cases:**

- ⚡ Volume without drive letter (mounted as folder)
- ⚡ CD-ROM drive with no disc → `type: "cdrom"`, minimal info
- ⚡ BitLocker encrypted volume → `bitlocker_status: "encrypted"`

---

### `storage.usage`

**Happy Path:**

- ✅ Returns all drives with space info
- ✅ `total_gb` is sum of all drive totals
- ✅ `path` parameter returns info for specific path's drive

**Edge Cases:**

- ⚡ `path` that doesn't exist → error
- ⚡ Network drive → included if mapped

---

### `storage.smart`

**Happy Path:**

- ✅ Returns SMART data for physical disk
- ✅ `status` is "OK" for healthy disk
- ✅ `attributes` array contains SMART attributes

**Error Handling:**

- ✅ SSD without traditional SMART → returns NVMe health info or partial data
- ✅ USB drive (no SMART) → error or empty attributes
- ✅ Virtual disk (Hyper-V, VMware) → no SMART data available, clear error

---

### `storage.partitions`

**Happy Path:**

- ✅ System disk has at least 1 partition
- ✅ EFI System partition exists on UEFI systems
- ✅ `type` is "GPT" or "MBR"
- ✅ Partitions' sizes sum to approximately disk size

---

## 9. `schedule.*`

### `schedule.list`

**Happy Path:**

- ✅ Returns list of tasks
- ✅ `include_microsoft: true` includes Microsoft tasks
- ✅ `include_microsoft: false` excludes `\Microsoft\` path tasks
- ✅ `include_disabled: false` excludes disabled tasks
- ✅ `folder: "\\"` lists root tasks
- ✅ `recursive: true` includes tasks in subfolders
- ✅ Each task has all required fields

**Edge Cases:**

- ⚡ Empty task folder → empty array
- ⚡ Task with multiple triggers
- ⚡ Task with multiple actions
- ⚡ Task running right now → `status: "running"`

---

### `schedule.info`

**Happy Path:**

- ✅ Returns full task definition for valid path
- ✅ Includes triggers, conditions, settings, history count
- ✅ `registration_date` is valid timestamp

**Error Handling:**

- ✅ Invalid task path → error "Task not found"

---

### `schedule.add`

**Happy Path:**

- 🎭 Creates daily task → appears in `schedule.list`
- 🎭 Creates weekly task with specific days
- 🎭 Creates boot trigger task
- 🎭 Creates logon trigger task
- 🎭 `next_run` is populated for time-based triggers

**Input Validation:**

- ✅ Empty name → error
- ✅ Invalid `trigger_type` → error
- ✅ `trigger_type: "daily"` without `start_time` → error
- ✅ `trigger_type: "weekly"` without `days_of_week` → error
- ✅ Non-existent `command` path → error
- ✅ Invalid `run_level` → error

**Security:**

- 🔒 Requires Operate tier
- 🔒 Cannot create task running as SYSTEM unless admin
- 🔒 Command path must be within allowed executable paths
- 🔒 Cannot schedule task that runs a shell command directly (must be executable path)

---

### `schedule.remove`

**Happy Path:**

- 🎭 Removes task → no longer in list

**Error Handling:**

- ✅ Non-existent path → error
- ✅ Currently running task → remove still works (task finishes current run)

---

### `schedule.run`

**Happy Path:**

- 🎭 Triggers immediate execution of scheduled task
- 🎭 Task `status` changes to "running"

**Error Handling:**

- ✅ Disabled task → error
- ✅ Task already running → error or starts new instance (depends on settings)

---

### `schedule.enable`

**Happy Path:**

- 🎭 Disabling enabled task → `enabled: false`
- 🎭 Enabling disabled task → `enabled: true`
- 🎭 Returns `previous_enabled` correctly

---

## 10. `security.*`

### `security.certs`

**Happy Path:**

- ✅ `store: "root"` returns trusted root CAs
- ✅ `store: "my"` returns machine personal certs
- ✅ `location: "machine"` vs `location: "user"` returns different stores
- ✅ Each cert has `subject`, `thumbprint`, `not_before`, `not_after`
- ✅ `is_expired` is accurate
- ✅ `days_until_expiry` is calculated correctly
- ✅ `expiring_within_days: 30` only returns certs expiring within 30 days

**Edge Cases:**

- ⚡ Self-signed cert → `self_signed: true`
- ⚡ Cert with no SAN
- ⚡ Cert with wildcard SAN
- ⚡ Expired cert → `is_expired: true`, negative `days_until_expiry`
- ⚡ Empty cert store → empty array

---

### `security.audit_policy`

**Happy Path:**

- ✅ Returns non-empty categories
- ✅ Contains "Account Logon", "Logon/Logoff", "Object Access"
- ✅ Each subcategory has `success` and `failure` booleans

---

### `security.local_policy`

**Happy Path:**

- ✅ Returns `password_policy` with `min_length`, `max_age_days`, etc.
- ✅ Returns `user_rights` array
- ✅ `complexity_required` is boolean

---

### `security.windows_update`

**Happy Path:**

- ✅ Returns `last_check` and `last_install` timestamps
- ✅ Returns `reboot_required` boolean
- ✅ `pending_updates` array may be empty
- ✅ `installed_updates` has up to `limit` entries

**Error Handling:**

- ✅ Windows Update service not running → error or empty result
- ✅ WSUS configured but unreachable → returns cached data or error

**Edge Cases:**

- ⚡ System with no updates ever installed
- ⚡ `pending_only: true` when no pending updates → empty pending array

---

### `security.defender`

**Happy Path:**

- ✅ Returns Defender status on system with Defender
- ✅ `antivirus_enabled` reflects real-time protection state
- ✅ `definition_date` is within recent past
- ✅ `recent_threats` array populated if threats detected

**Error Handling:**

- ✅ Defender not installed (third-party AV) → error or alternative AV info
- ✅ Defender service not running → `antivirus_enabled: false`

---

## 11. `container.*`

### `container.list`

**Happy Path:**

- ✅ Returns containers when Docker is running
- ✅ `all: false` only returns running containers
- ✅ `all: true` includes stopped containers
- ✅ `filter_name` matches by container name (contains)
- ✅ `filter_image` matches by image name
- ✅ `filter_status: "running"` returns only running
- ✅ Each container has `id`, `name`, `image`, `state`
- ✅ Port mappings are correctly listed
- ✅ `count` matches `containers.length`

**Error Handling:**

- ✅ Docker not installed → error "Docker not available"
- ✅ Docker daemon not running → error "Docker daemon not running"
- ✅ Docker named pipe not accessible → error "Cannot connect to Docker"

**Edge Cases:**

- ⚡ Container with no port mappings → empty `ports` array
- ⚡ Container with many labels → all returned
- ⚡ Container in "restarting" state
- ⚡ Windows container vs Linux container → both listed
- ⚡ `filter_name` and `filter_status` combined → AND logic

---

### `container.inspect`

**Happy Path:**

- ✅ Returns full details for valid container ID
- ✅ Returns full details for valid container name
- ✅ `environment` array lists all env vars
- ✅ `health_check` populated for container with health check
- ✅ `platform` shows "linux" or "windows"

**Error Handling:**

- ✅ Invalid container ID → error "Container not found"
- ✅ Partial container ID → matches if unique
- ✅ Ambiguous partial ID → error "Multiple containers match"

**Edge Cases:**

- ⚡ Container with no health check → `health_check: null`
- ⚡ Container with restart policy → shown in `restart_policy`
- ⚡ Container with resource limits → `resource_limits` populated

---

### `container.logs`

**Happy Path:**

- ✅ Returns stdout logs for running container
- ✅ `tail: 10` returns last 10 lines
- ✅ `since` filters by time
- ✅ `stderr: false` excludes stderr
- ✅ `stdout: false` excludes stdout

**Error Handling:**

- ✅ Container not found → error
- ✅ Container with no logs (just started) → empty string

**Edge Cases:**

- ⚡ Very large logs (>100MB) → respects `tail` limit
- ⚡ Binary output in logs → handled without crash
- ⚡ `tail: 0` → empty or error

---

### `container.exec`

**Happy Path:**

- 🎭 Executes simple command → returns stdout
- 🎭 Failed command → returns non-zero `exit_code` and stderr
- 🎭 `user` parameter runs as specified user
- 🎭 `working_dir` changes execution directory
- 🎭 `env` injects additional environment variables

**Input Validation:**

- ✅ Empty `command` array → error
- ✅ Non-existent container → error

**Error Handling:**

- ✅ Container not running → error "Container is not running"
- ✅ Command not found in container → non-zero exit code with stderr
- ✅ Command timeout → error (configurable)

**Security:**

- 🔒 Requires Operate tier
- 🔒 Command and output logged in audit trail
- 🔒 Cannot execute interactive/TTY sessions

**Edge Cases:**

- ⚡ Command with special characters in arguments
- ⚡ Very large stdout output → truncated at configurable limit
- ⚡ Long-running command → completes or times out

---

### `container.start` / `container.stop` / `container.restart`

**Happy Path:**

- 🎭 Start stopped container → `state: "running"`
- 🎭 Stop running container → `state: "exited"`
- 🎭 Restart running container → `state: "running"`, new uptime

**Error Handling:**

- ✅ Start already running → error "Container already running"
- ✅ Stop already stopped → error "Container is not running"
- ✅ Stop timeout → force kill after `timeout_seconds`
- ✅ Container not found → error

**Security:**

- 🔒 Requires Operate tier

---

### `container.images`

**Happy Path:**

- ✅ Returns list of images
- ✅ Each image has `id`, `repository`, `tag`, `size_mb`
- ✅ `filter_name` filters by repository
- ✅ `dangling: true` only shows untagged images

**Error Handling:**

- ✅ Docker not running → error
- ✅ No images → empty array

---

## 12. `hardware.*`

### `hardware.pci`

**Happy Path:**

- ✅ Returns non-empty list (system always has PCI devices)
- ✅ Contains display adapter
- ✅ Contains storage controller
- ✅ `class: "display"` filters to display adapters only
- ✅ `class: "network"` filters to network adapters
- ✅ Each device has `name`, `device_id`, `status`
- ✅ `driver_version` populated for devices with drivers

**Edge Cases:**

- ⚡ Device with no driver → `driver_version: null`
- ⚡ Disabled device → `status: "disabled"`
- ⚡ Virtual machine with few PCI devices
- ⚡ `class` filter with no matches → empty array

---

### `hardware.usb`

**Happy Path:**

- ✅ Returns list of USB devices (may be empty in VM)
- ✅ Each device has `name`, `device_id`
- ✅ `class` identifies device type

**Edge Cases:**

- ⚡ No USB devices (virtual machine) → empty array
- ⚡ USB hub (parent device) appears in list
- ⚡ USB device without manufacturer info → `manufacturer: null`

---

### `hardware.bios`

**Happy Path:**

- ✅ Returns `bios_vendor` non-empty
- ✅ Returns `bios_version` non-empty
- ✅ Returns `bios_mode` as "UEFI" or "Legacy"
- ✅ Returns `secure_boot` boolean
- ✅ Returns `motherboard_manufacturer` and `motherboard_product`
- ✅ Returns `system_serial` (may be "To Be Filled" in VMs)
- ✅ Returns valid `uuid`

**Edge Cases:**

- ⚡ Virtual machine → vendor is "Hyper-V", "VMware", "QEMU"
- ⚡ Fields filled with "To Be Filled By O.E.M." → returned as-is
- ⚡ `secure_boot` on Legacy BIOS → `false`

---

### `hardware.memory`

**Happy Path:**

- ✅ Returns at least one memory module
- ✅ `total_capacity_gb` > 0
- ✅ `used_slots` <= `total_slots`
- ✅ Sum of module capacities ≈ `total_capacity_gb`
- ✅ Each module has `speed_mhz`, `type`

**Edge Cases:**

- ⚡ Virtual machine → single module reported, may lack manufacturer info
- ⚡ System with empty DIMM slots → shown in `total_slots` but not in `modules`
- ⚡ Mixed memory speeds → each module shows its own speed

---

### `hardware.gpu`

**Happy Path:**

- ✅ Returns at least one adapter (even basic display)
- ✅ `name` is non-empty
- ✅ `vram_bytes` > 0 for discrete GPU
- ✅ `resolution` in format "WIDTHxHEIGHT"
- ✅ `driver_version` is non-empty

**Edge Cases:**

- ⚡ Server Core (no GUI) → basic display adapter or none
- ⚡ Remote Desktop session → RDP display adapter
- ⚡ Multiple GPUs → all listed
- ⚡ Hyper-V VM → "Microsoft Hyper-V Video" adapter

---

### `hardware.battery`

**Happy Path:**

- ✅ Laptop → returns battery info with `charge_percent`
- ✅ `on_ac_power` correctly reflects power state
- ✅ `health_percent` calculated correctly

**Error Handling:**

- ✅ Desktop / Server (no battery) → empty `batteries` array, `on_ac_power: true`
- ✅ UPS battery → may appear depending on driver

**Edge Cases:**

- ⚡ Battery fully charged → `status: "full"`, `charge_percent: 100`
- ⚡ Battery critically low → `charge_percent` < 5
- ⚡ No battery firmware info → null fields

---

## 13. `time.*`

### `time.info`

**Happy Path:**

- ✅ `local_time` and `utc_time` are valid ISO 8601
- ✅ `local_time` - `utc_offset` ≈ `utc_time` (within 2 seconds)
- ✅ `timezone_id` is valid Windows timezone
- ✅ `daylight_saving` reflects current DST state
- ✅ `ntp_server` is configured (default: time.windows.com)
- ✅ `source` is "ntp" or "domain" depending on machine

**Edge Cases:**

- ⚡ UTC timezone → `utc_offset: "+00:00"`
- ⚡ Timezone with half-hour offset (e.g., India Standard Time +05:30)
- ⚡ During DST transition period
- ⚡ Domain-joined machine → `source: "domain"`

---

### `time.sync`

**Happy Path:**

- 🎭 Forces NTP sync → `synced: true`
- 🎭 Returns `offset_ms` showing correction
- 🎭 Returns `source` showing NTP server used

**Error Handling:**

- ✅ NTP server unreachable → error
- ✅ W32Time service not running → error

**Security:**

- 🔒 Requires Operate tier

---

### `time.set_timezone`

**Happy Path:**

- 🎭 Changes timezone → returns previous and new
- 🎭 `local_time` in response reflects new timezone

**Input Validation:**

- ✅ Invalid timezone ID → error "Timezone not found"

**Security:**

- 🔒 Requires Operate tier

---

## 14. `registry.*`

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

### `registry.export`

**Happy Path:**

- ✅ Returns valid .reg format content
- ✅ Exported content re-importable via `reg import`
- ✅ Includes all subkeys and values recursively
- ✅ `keys_exported` and `values_exported` counts are correct

**Security:**

- 🔒 Cannot export blocked keys

---

## 15. `iis.*`

### `iis.sites`

**Happy Path:**

- ✅ Returns list of websites
- ✅ Default Web Site exists on fresh IIS install
- ✅ Each site has `bindings`, `physical_path`, `application_pool`
- ✅ Running site → `state: "started"`

**Error Handling:**

- ✅ IIS not installed → error "IIS not available"
- ✅ IIS management tools not installed → error

---

### `iis.pools`

**Happy Path:**

- ✅ Returns list of application pools
- ✅ DefaultAppPool exists on fresh IIS
- ✅ Each pool has `runtime_version`, `pipeline_mode`, `state`
- ✅ `worker_processes` count is accurate

---

### `iis.site.start` / `iis.site.stop`

**Happy Path:**

- 🎭 Stop running site → `state: "stopped"`
- 🎭 Start stopped site → `state: "started"`

**Error Handling:**

- ✅ Start already started → error
- ✅ Stop already stopped → error
- ✅ Start with port conflict → error (port in use by another site/process)
- ✅ Non-existent site → error

**Security:**

- 🔒 Requires Operate tier

---

### `iis.pool.start` / `iis.pool.stop` / `iis.pool.recycle`

**Happy Path:**

- 🎭 Stop pool → worker processes terminate
- 🎭 Start pool → pool available
- 🎭 Recycle → `previous_pid` differs from new worker PID

**Error Handling:**

- ✅ Recycle stopped pool → error
- ✅ Non-existent pool → error

---

### `iis.site.config`

**Happy Path:**

- ✅ Returns complete site configuration
- ✅ `bindings` include protocol, host, port, IP
- ✅ `authentication` shows enabled/disabled state for each method
- ✅ `default_documents` lists document order
- ✅ `logging` shows log configuration

**Error Handling:**

- ✅ Non-existent site → error

---

### `iis.site.config.set`

**Happy Path:**

- 🎭 Add binding → appears in site config
- 🎭 Change physical path → reflected in config
- 🎭 Enable/disable authentication method

**Input Validation:**

- ✅ Invalid `setting` → error
- ✅ Invalid binding format → error
- ✅ Physical path doesn't exist → error

**Security:**

- 🔒 Requires Operate tier
- 🔒 Physical path must be within allowed paths
- 🔒 Cannot bind to ports < 1024 without elevated policy (configurable)

---

### `iis.pool.config` / `iis.pool.config.set`

**Happy Path:**

- ✅ Returns detailed pool configuration
- 🎭 Changes runtime version
- 🎭 Changes identity type
- 🎭 Changes recycle interval

**Edge Cases:**

- ⚡ Setting "No Managed Code" as runtime → `runtime_version: ""`
- ⚡ Custom identity requires username/password validation

---

### `iis.worker_processes`

**Happy Path:**

- ✅ Returns active worker processes for running pools
- ✅ PID matches actual w3wp.exe process
- ✅ `pool_name` correctly identifies the pool

**Edge Cases:**

- ⚡ No active workers (all pools idle) → empty array
- ⚡ Pool with multiple workers (web garden) → all listed

---

## 16. `ad.*`

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

### `ad.groups`

**Happy Path:**

- 🧪 Returns AD groups
- 🧪 `type: "security"` filters to security groups
- 🧪 `type: "distribution"` filters to distribution groups
- 🧪 `scope: "global"` filters by group scope
- 🧪 Each group has `member_count`

**Security:**

- 🔒 LDAP injection protection on `filter`

**Edge Cases:**

- ⚡ Group with >1000 members → `member_count` is correct (uses ranged retrieval)
- ⚡ Built-in groups (Domain Admins, etc.) → included with correct type

---

### `ad.user.info`

**Happy Path:**

- 🧪 Returns full details for valid SAM account name
- 🧪 Returns full details for valid UPN
- 🧪 Returns full details for valid DN
- 🧪 `all_groups` includes recursive nested memberships
- 🧪 `uac_flags` decoded correctly (NORMAL_ACCOUNT, DONT_EXPIRE_PASSWD, etc.)
- 🧪 `sid` is valid SID string format (S-1-5-21-...)

**Error Handling:**

- ✅ User not found → error
- ✅ Ambiguous match → error with candidates

**Edge Cases:**

- ⚡ User with many group memberships (>100)
- ⚡ User with account expiration set → `account_expires` populated
- ⚡ User locked out → `locked_out: true`, `lockout_time` populated
- ⚡ User with no manager → `manager: null`

---

### `ad.user.groups`

**Happy Path:**

- 🧪 `recursive: true` includes nested groups
- 🧪 `recursive: false` only direct memberships
- 🧪 `direct` flag distinguishes direct vs nested
- 🧪 Count matches array length

**Edge Cases:**

- ⚡ User with circular group nesting → handled without infinite loop
- ⚡ User with only primary group (Domain Users) → appears in list
- ⚡ `recursive: true` on user with deep nesting (>10 levels)

---

### `ad.group.members`

**Happy Path:**

- 🧪 Returns members of specified group
- 🧪 `recursive: true` includes nested group members
- 🧪 Members include users, groups, and computers
- 🧪 Each member has `type` identifying user/group/computer

**Error Handling:**

- ✅ Group not found → error
- ✅ Empty group → empty array

**Edge Cases:**

- ⚡ Group with >1500 members → uses ranged retrieval (AD limit)
- ⚡ Nested group appears as member when `recursive: false`
- ⚡ Foreign security principal as member (from trusted domain)

---

### `ad.computers`

**Happy Path:**

- 🧪 Returns domain computers
- 🧪 `os_filter: "Server 2022"` filters correctly
- 🧪 `stale_days: 90` only returns computers not logged on in 90 days
- 🧪 Each computer has `dns_hostname`, `operating_system`

**Edge Cases:**

- ⚡ Computer with no last logon (just joined domain)
- ⚡ Computer account disabled → `enabled: false`
- ⚡ Computer with no OS info (pre-join state)

---

### `ad.ou.list`

**Happy Path:**

- 🧪 Returns OU tree from domain root
- 🧪 `root` parameter starts from specific OU
- 🧪 `depth: 1` only immediate children
- 🧪 Each OU has child counts (users, computers, groups)
- 🧪 `gpo_links` lists linked GPOs

---

### `ad.user.enable` / `ad.user.disable`

**Happy Path:**

- 🧪 Enable disabled user → `enabled: true`
- 🧪 Disable enabled user → `enabled: false`
- 🧪 Returns `previously_enabled` state

**Error Handling:**

- ✅ User not found → error
- ✅ Insufficient permissions → error
- ✅ Already in desired state → succeeds, `previously_enabled` matches target

**Security:**

- 🔒 Cannot disable Domain Admin accounts without elevated policy
- 🔒 Cannot disable own account
- 🔒 Action logged in audit trail

---

### `ad.user.unlock`

**Happy Path:**

- 🧪 Unlocks locked user → `was_locked: true`, `unlocked: true`

**Error Handling:**

- ✅ User not locked → `was_locked: false`, `unlocked: true`
- ✅ User not found → error

---

### `ad.user.resetpw`

**Happy Path:**

- 🧪 Resets password → user can log in with new password
- 🧪 `must_change: true` → user forced to change on next logon

**Input Validation:**

- ✅ Empty password → error
- ✅ Password doesn't meet domain complexity requirements → error

**Security:**

- 🔒 Requires Dangerous tier
- 🔒 Password NEVER appears in audit log (only the fact of reset)
- 🔒 Cannot reset password of higher-privilege account (Domain Admin resetting Enterprise Admin)
- 🔒 Old password not needed (admin reset, not change)
- 🔒 Rate limited (prevent brute-force reset attacks)

**Edge Cases:**

- ⚡ Password with Unicode characters
- ⚡ Very long password (>128 chars)
- ⚡ Reset for user with "cannot change password" flag → still works (admin reset)

---

## 17. `hyperv.*`

### `hyperv.vms`

**Happy Path:**

- 🧪 Returns list of VMs
- 🧪 `state: "running"` only returns running VMs
- 🧪 `state: "off"` only returns off VMs
- 🧪 Each VM has `id`, `name`, `state`, `generation`
- 🧪 Running VM has `memory_assigned_mb` > 0

**Error Handling:**

- ✅ Hyper-V not installed → error "Hyper-V not available"
- ✅ Hyper-V not enabled → error
- ✅ No VMs → empty array

---

### `hyperv.vm.info`

**Happy Path:**

- 🧪 Returns full details for valid VM name
- 🧪 Returns full details for valid VM GUID
- 🧪 `disks` array lists VHD/VHDX with paths and sizes
- 🧪 `network_adapters` lists adapters with switch assignments
- 🧪 Running VM → `cpu_usage_percent` populated
- 🧪 Running VM → `guest_os` detected via integration services

**Error Handling:**

- ✅ VM not found → error
- ✅ VM with no disks → empty `disks` array
- ✅ VM with no network → empty `network_adapters`

**Edge Cases:**

- ⚡ Generation 1 vs Generation 2 → different capabilities shown
- ⚡ VM with multiple disks on different controllers
- ⚡ VM with ISO mounted → appears in `dvd_drives`
- ⚡ VM with snapshots → `snapshots` array populated
- ⚡ VM with dynamic memory → `memory_assigned_mb` may differ from `memory_startup_mb`

---

### `hyperv.vm.start`

**Happy Path:**

- 🧪 Start off VM → `state: "running"`, `started: true`

**Error Handling:**

- ✅ Already running → error
- ✅ VM in saved state → start resumes from saved state
- ✅ VM with missing VHD → error "Virtual hard disk not found"
- ✅ VM config corrupted → error
- ✅ Insufficient resources (memory) → error

**Security:**

- 🔒 Requires Domain tier

---

### `hyperv.vm.stop`

**Happy Path:**

- 🧪 `force: false` → graceful shutdown via integration services
- 🧪 `force: true` → immediate turn off

**Error Handling:**

- ✅ Already off → error
- ✅ Guest doesn't respond to shutdown (no IC) → timeout, suggest force
- ✅ VM in paused state → can be stopped

**Edge Cases:**

- ⚡ VM without integration services → graceful shutdown fails, need force
- ⚡ VM running critical workload → warning before force

---

### `hyperv.vm.restart`

**Happy Path:**

- 🧪 `force: false` → graceful restart
- 🧪 `force: true` → hard reset

**Error Handling:**

- ✅ VM is off → error (can't restart what's not running)

---

### `hyperv.vm.snapshot`

**Happy Path:**

- 🧪 Creates checkpoint → `snapshot_id` returned
- 🧪 Custom name → `snapshot_name` matches
- 🧪 Auto-generated name includes timestamp
- 🧪 Snapshot appears in `hyperv.vm.info` snapshots list

**Error Handling:**

- ✅ Insufficient disk space → error
- ✅ VM not found → error

**Edge Cases:**

- ⚡ Snapshot while VM is running (online checkpoint)
- ⚡ Snapshot of off VM (offline checkpoint)
- ⚡ Multiple snapshots → tree maintained correctly

---

### `hyperv.switches`

**Happy Path:**

- 🧪 Lists virtual switches
- 🧪 Each switch has `type`, `interface` (for external)
- 🧪 `connected_vms` count is accurate

**Edge Cases:**

- ⚡ No switches configured → empty array
- ⚡ Default switch (auto-created) → listed
- ⚡ External switch bound to team NIC

---

## 18. `gpo.*`

### `gpo.list`

**Happy Path:**

- 🧪 Returns applied GPOs
- 🧪 `target: "computer"` returns only computer GPOs
- 🧪 `target: "user"` returns only user GPOs
- 🧪 Each GPO has `name`, `guid`, `status`
- 🧪 `link_order` reflects priority

**Error Handling:**

- ✅ Not domain-joined → error "Machine is not domain-joined"
- ✅ No GPOs applied → empty arrays

**Edge Cases:**

- ⚡ Enforced GPO → `enforced: true`
- ⚡ GPO with WMI filter → `wmi_filter` populated
- ⚡ Denied GPO (security filtering) → `status: "denied"`

---

### `gpo.result`

**Happy Path:**

- 🧪 Returns Resultant Set of Policy
- 🧪 `computer_settings` grouped by category
- 🧪 `user_settings` grouped by category
- 🧪 `security_groups` lists effective groups
- 🧪 `format: "full"` returns more detail than "summary"

**Error Handling:**

- ✅ RSOP calculation fails → error with reason
- ✅ WMI RSOP namespace not available → error

**Edge Cases:**

- ⚡ Conflicting policies → winner is shown with source GPO
- ⚡ Many GPOs (>20) → all processed

---

### `gpo.update`

**Happy Path:**

- 🧪 `target: "both"` refreshes computer and user policy
- 🧪 `force: true` reapplies all policies
- 🧪 Returns success for both targets

**Error Handling:**

- ✅ Not domain-joined → error
- ✅ Domain controller unreachable → error (policies remain cached)

**Security:**

- 🔒 Requires Operate tier

---

## 19. `printer.*`

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

### `printer.info`

**Happy Path:**

- ✅ Returns complete info for valid printer name
- ✅ `paper_sizes` lists supported sizes
- ✅ `resolutions` lists supported DPI
- ✅ `total_pages_printed` and `total_jobs_printed` are non-negative
- ✅ `port_name` matches actual port

**Error Handling:**

- ✅ Non-existent printer → error "Printer not found"
- ✅ Printer name is case-insensitive on Windows

**Edge Cases:**

- ⚡ Network printer with server offline → partial info, `status: "offline"`
- ⚡ Printer with last error → `last_error` populated
- ⚡ Printer shared on network → `is_shared: true`, `share_name` populated

---

### `printer.jobs`

**Happy Path:**

- ✅ Returns jobs for specified printer
- ✅ No `printer` specified → returns jobs from all printers
- ✅ `status: "pending"` filters pending jobs
- ✅ `status: "printing"` shows currently printing job
- ✅ `status: "error"` shows errored jobs
- ✅ Each job has `document`, `owner`, `submitted`, `size_bytes`
- ✅ Jobs ordered by position in queue
- ✅ `total_count` matches array length

**Error Handling:**

- ✅ No jobs → empty array
- ✅ Non-existent printer → error

**Edge Cases:**

- ⚡ Job in "deleting" state → transient, may not appear
- ⚡ Job with 0 `total_pages` (unknown page count)
- ⚡ Very large job (>100MB) → `size_bytes` is correct
- ⚡ Job from network user → `owner` includes domain
- ⚡ Many jobs (>100 in queue)

---

### `printer.job.cancel`

**Happy Path:**

- 🎭 Cancels pending job → removed from queue
- 🎭 Returns `document` name of cancelled job

**Error Handling:**

- ✅ Job not found → error
- ✅ Job already completed → error "Job not found" or "Job already complete"
- ✅ Job owned by different user (without admin) → error "Access denied"
- ✅ Job currently printing → cancelled (may partial print)

**Security:**

- 🔒 Requires Operate tier
- 🔒 Action logged with job details and owner

---

### `printer.job.pause` / `printer.job.resume`

**Happy Path:**

- 🎭 Pause pending job → job stays in queue but doesn't print
- 🎭 Resume paused job → job resumes printing
- 🎭 Returns correct status

**Error Handling:**

- ✅ Pause already paused → error or idempotent
- ✅ Resume non-paused → error or idempotent
- ✅ Job not found → error

---

### `printer.queue.clear`

**Happy Path:**

- 🎭 Cancels all jobs in specified printer queue
- 🎭 Returns count of jobs cancelled
- 🎭 Queue is empty after clearing

**Error Handling:**

- ✅ Already empty queue → `jobs_cancelled: 0`
- ✅ Non-existent printer → error

**Security:**

- 🔒 Requires Operate tier
- 🔒 Logged with printer name and job count

**Edge Cases:**

- ⚡ Clearing queue while job is actively printing → active job may complete or cancel
- ⚡ Queue with 100+ jobs → all cancelled
- ⚡ Jobs owned by different users → all cancelled (admin privilege)

---

### `printer.spooler.status`

**Happy Path:**

- ✅ Running spooler → `status: "running"`, `pid` populated
- ✅ Returns `spool_directory` path
- ✅ Returns `spool_size_mb` ≥ 0
- ✅ Returns `temp_files_count` ≥ 0

**Error Handling:**

- ✅ Spooler stopped → `status: "stopped"`, `pid: null`

**Edge Cases:**

- ⚡ Spool directory with residual files (from crashed jobs) → counted in `temp_files_count`
- ⚡ Spool directory permission denied → error reading size

---

### `printer.spooler.restart`

**Happy Path:**

- 🎭 Restarts spooler → `restarted: true`
- 🎭 `clear_queue: true` → `spool_files_cleared` > 0 (if files existed)
- 🎭 `clear_queue: false` → jobs preserved
- 🎭 Returns `elapsed_ms` > 0

**Error Handling:**

- ✅ Spooler fails to restart → error
- ✅ Spool files locked → cleared after service stop, before start

**Security:**

- 🔒 Requires Operate tier
- 🔒 Logged in audit trail

**Edge Cases:**

- ⚡ Restart while jobs are actively printing → jobs re-queue or fail
- ⚡ Restart with `clear_queue: true` → in-progress jobs are lost
- ⚡ Very large spool directory (>1GB) → clearing takes time

---

### `printer.spooler.clear`

**Happy Path:**

- 🎭 Stops spooler, deletes .SHD and .SPL files, restarts spooler
- 🎭 Returns `files_deleted` count and `bytes_freed`

**Error Handling:**

- ✅ Spooler won't stop → error
- ✅ Some spool files locked → best-effort deletion, report count
- ✅ Spooler won't restart after clearing → error (critical)

**Security:**

- 🔒 Requires Operate tier
- 🔒 Warning: this destroys all pending print jobs

**Edge Cases:**

- ⚡ Empty spool directory → `files_deleted: 0`, `bytes_freed: 0`
- ⚡ Corrupted spool files (cause of stuck spooler) → successfully deleted

---

### `printer.pause` / `printer.resume`

**Happy Path:**

- 🎭 Pause printer → `status: "paused"`, no new jobs print
- 🎭 Resume printer → jobs start printing again
- 🎭 `pending_jobs` count reflects queue depth

**Error Handling:**

- ✅ Pause already paused → idempotent or error
- ✅ Resume not paused → idempotent or error
- ✅ Non-existent printer → error

---

### `printer.test`

**Happy Path:**

- 🎭 Sends test page → `test_page_sent: true`
- 🎭 Returns `job_id` for the test page job
- 🎭 Test page job appears in `printer.jobs`

**Error Handling:**

- ✅ Printer offline → error
- ✅ Printer in error state → error
- ✅ Virtual printer → test page generates file

---

### `printer.set_default`

**Happy Path:**

- 🎭 Changes default printer → `previous_default` returned
- 🎭 `printer.list` now shows new default with `is_default: true`

**Error Handling:**

- ✅ Non-existent printer → error
- ✅ Already default → succeeds, `previous_default` == `name`

---

### `printer.drivers`

**Happy Path:**

- ✅ Returns installed printer drivers
- ✅ Each driver has `name`, `version`, `architecture`
- ✅ `used_by` lists printers using each driver

**Edge Cases:**

- ⚡ Driver with no printers using it
- ⚡ Multiple versions of same driver
- ⚡ x86 and x64 versions → both listed with correct `architecture`

---

### `printer.ports`

**Happy Path:**

- ✅ Returns printer ports
- ✅ TCP port has `address` and `port_number`
- ✅ USB port identified as `type: "usb"`
- ✅ `used_by` lists printers on each port

**Edge Cases:**

- ⚡ WSD port (Web Services for Devices) → `type: "wsd"`
- ⚡ Shared port (multiple printers) → `used_by` has multiple entries
- ⚡ Port with no printer → empty `used_by`

---

## Test Infrastructure Requirements

### Test Categories and Execution

| Category          | Symbol | Requires                   | CI/CD Feasible | Execution        |
| ----------------- | ------ | -------------------------- | -------------- | ---------------- |
| Input Validation  | ✅     | Nothing                    | Yes            | Every build      |
| Happy Path (mock) | 🎭     | Mocked OS APIs             | Yes            | Every build      |
| Security          | 🔒     | Nothing (validation logic) | Yes            | Every build      |
| Edge Cases        | ⚡     | Varies                     | Mostly yes     | Every build      |
| Integration       | 🧪     | Real Windows + services    | Partial        | Nightly / manual |

### Mock Strategy

**What to mock:**

- WMI/CIM queries → return predefined `ManagementObject` collections
- Registry access → in-memory registry tree
- Service Control Manager → simulated service states
- Event Log → in-memory event store
- Docker API → HTTP mock server on named pipe
- Active Directory → LDAP mock server or in-memory DirectoryEntry
- IIS ServerManager → mocked site/pool objects
- File system → in-memory filesystem or restricted temp directory
- Hyper-V WMI → predefined VM objects

**What NOT to mock (integration tests):**

- Actual service start/stop (use a dedicated test service)
- Actual file I/O (use temp directory within allowed paths)
- Actual process management (use test child processes)
- Actual Docker commands (use dedicated test containers)
- Actual AD operations (use test domain or AD LDS)
- Actual printer operations (use virtual PDF printer)

### Test Service for service.\* Tests

Create a dedicated Windows Service (`mcpw-test-svc`) that:

- Installs easily via `sc.exe create`
- Can be started/stopped/paused reliably
- Logs to a known Event Log source
- Has configurable startup behavior (fast, slow, crash-on-start)
- Cleans up after itself

### Test Data Fixtures

Maintain fixtures for:

- **WMI responses**: JSON files with typical `Win32_*` class instances
- **Event Log entries**: Synthetic events for search/filter testing
- **Registry trees**: In-memory registry with known values
- **AD objects**: Mock users, groups, OUs, computers with known relationships
- **Docker API responses**: Container list, inspect, logs payloads
- **IIS configurations**: Site/pool definitions with various configurations
- **Print job data**: Queue entries with various states

### Performance Benchmarks

Each tool should also have a basic performance test:

- ✅ `system.info` completes in < 2 seconds
- ✅ `process.list` (no filter) completes in < 3 seconds
- ✅ `service.list` completes in < 2 seconds
- ✅ `log.tail` (50 entries) completes in < 1 second
- ✅ `network.interfaces` completes in < 1 second
- ✅ `file.read` (1MB file) completes in < 500ms
- ✅ `registry.get` completes in < 100ms
- ✅ `ad.users` (100 users) completes in < 5 seconds
- ✅ `printer.list` completes in < 2 seconds
- ✅ Tool with no matches/empty result completes in < 500ms

---

## Summary

| Domain        | Tools   | Tests (approx) |
| ------------- | ------- | -------------- |
| Global        | —       | 25             |
| `system.*`    | 6       | 65             |
| `process.*`   | 6       | 85             |
| `service.*`   | 8       | 80             |
| `log.*`       | 5       | 60             |
| `network.*`   | 10      | 100            |
| `file.*`      | 13      | 150            |
| `identity.*`  | 5       | 50             |
| `storage.*`   | 5       | 40             |
| `schedule.*`  | 6       | 50             |
| `security.*`  | 5       | 40             |
| `container.*` | 8       | 70             |
| `hardware.*`  | 6       | 45             |
| `time.*`      | 3       | 25             |
| `registry.*`  | 6       | 80             |
| `iis.*`       | 11      | 65             |
| `ad.*`        | 11      | 95             |
| `hyperv.*`    | 6       | 50             |
| `gpo.*`       | 3       | 25             |
| `printer.*`   | 16      | 110            |
| **Total**     | **139** | **~1,310**     |
