# mcpw — Implementation Tasks (TDD)

> Master task list for implementing all 139 MCP tools across 19 domains.
> Methodology: **Test-Driven Development** + **Self-Review** for .NET, Windows, and MCP best practices.

---

## Methodology

### TDD Cycle (per tool)

1. **Red** — Write failing unit tests (from specs/tests.md) covering input validation, happy path (mocked), security, and edge cases
2. **Green** — Implement the minimum code to pass all tests
3. **Refactor** — Clean up without changing behavior; ensure no duplication across tools in the same domain
4. **Self-Review** — Apply the checklist below before marking complete

### Self-Review Checklist

#### .NET Best Practices

- [ ] Async/await used throughout (no `.Result` or `.Wait()`)
- [ ] `CancellationToken` propagated to all async calls
- [ ] `IDisposable` resources wrapped in `using` statements
- [ ] No allocations in hot paths (use `Span<T>`, `ArrayPool` where relevant)
- [ ] `sealed` classes where inheritance is not intended
- [ ] Nullable reference types respected — no `null!` suppressions without justification
- [ ] `TreatWarningsAsErrors` passes with zero warnings
- [ ] Record types / POCOs for data; no mutable state in tool handlers

#### Windows Best Practices

- [ ] All OS calls go through interfaces (`IWmiClient`, `IRegistryAccess`, etc.)
- [ ] WMI queries use parameterized WQL — no string interpolation
- [ ] PowerShell runs in Constrained Language Mode
- [ ] Registry blocked hives (SAM, SECURITY, Lsa\Secrets) rejected before access
- [ ] Path validation: canonicalization → traversal check → blocklist check → allowlist check
- [ ] Unicode normalization applied before path comparison
- [ ] Alternate Data Streams, device paths, UNC paths blocked unless explicitly allowed
- [ ] Service operations have timeout guards (no infinite waits)
- [ ] COM objects released deterministically (Marshal.ReleaseComObject)

#### MCP Protocol Best Practices

- [ ] JSON-RPC 2.0 response structure for every response (id, result/error)
- [ ] Error codes follow spec: -32601 MethodNotFound, -32602 InvalidParams, -32603 InternalError
- [ ] No stack traces leaked in error responses
- [ ] No raw .NET exception messages exposed to client
- [ ] Tool manifest (`tools/list`) includes accurate inputSchema for every tool
- [ ] All timestamps are ISO 8601 UTC
- [ ] Optional fields present as `null`, not omitted
- [ ] Extra/unknown parameters ignored (not rejected)
- [ ] Tool names are case-sensitive and match spec exactly

#### Security Best Practices

- [ ] Input validated at system boundary (tool entry point)
- [ ] Injection characters rejected: `` ` ; | & $ \0 \r \n ``
- [ ] LDAP special characters escaped in AD queries
- [ ] XPath special characters escaped in Event Log queries
- [ ] Privilege tier enforced before any OS call
- [ ] Audit trail entry for all mutating operations
- [ ] Passwords/secrets never appear in logs or error messages
- [ ] Strings > 64KB rejected
- [ ] Null bytes in any string input rejected

---

## Notation

- `[T]` = Test task (write tests first)
- `[I]` = Implementation task
- `[R]` = Refactor / review task
- `✅` / `🔒` / `⚡` / `🎭` / `🧪` = test categories from specs/tests.md
- **Blocked by** = cannot start until dependency is done
- **Parallel** = can run simultaneously with other marked tasks

---

## Phase 0: Infrastructure & Core (Foundation)

> Everything else depends on this phase. No tool work can begin until Phase 0 is complete.

### 0.1 MCP Types & JSON-RPC

- [ ] `[T]` **JsonRpcMessageTests** — Verify serialization/deserialization of `JsonRpcRequest`, `JsonRpcResponse`, `JsonRpcError` (already started in `tests/Protocol/JsonRpcMessageTests.cs`)
  - ✅ Valid request round-trips
  - ✅ Error response includes code + message, no data leak
  - ✅ Null id handled
  - ✅ Params as object or array
- [ ] `[I]` **McpTypes.cs** — Complete all MCP protocol types: `McpInitializeResult`, `McpCapabilities`, `McpToolsCapability`, `McpServerInfo`, `McpToolDefinition`, `McpCallToolParams`, `McpCallToolResult`, `McpToolsListResult`, `McpJson` helper, `RpcErrorCodes`
  - File: `src/Mcpw/Types/McpTypes.cs`
- [ ] `[R]` Review: JSON naming policy (`snake_case`), nullable annotations, `JsonSerializerOptions` singleton reuse

### 0.2 Server (JSON-RPC Router)

- [ ] `[T]` **ServerTests** — (already started in `tests/ServerTests.cs`)
  - ✅ `initialize` returns protocol version and capabilities
  - ✅ `tools/list` returns enabled tools only
  - ✅ `tools/call` routes to correct handler domain
  - ✅ Unknown method → -32601 MethodNotFound
  - ✅ Missing tool name → -32602 InvalidParams
  - ✅ Tool in disabled domain → -32601
  - ✅ Malformed JSON → -32700 ParseError
  - ✅ Handler throws `ArgumentException` → error result (not crash)
  - ✅ Handler throws `UnauthorizedAccessException` → access denied result
  - ✅ Handler throws unexpected exception → -32603 with safe message
  - ✅ `notifications/initialized` → no response (empty string)
  - ✅ Extra params ignored
  - ✅ Concurrent calls do not race
- [ ] `[I]` **McpServer** finalization — Ensure `HandleLineAsync`, `RunAsync`, all method handlers match spec
  - File: `src/Mcpw/Server.cs`
- [ ] `[R]` Review: no `catch(Exception)` swallowing, thread safety, CancellationToken plumbing

### 0.3 Configuration

- [ ] `[T]` **ConfigTests** — (already started in `tests/ConfigTests.cs`)
  - ✅ Missing config file → safe defaults
  - ✅ Valid config → all properties loaded
  - ✅ `enabledDomains` controls `IsDomainEnabled()`
  - ✅ `disabledDomains` overrides `enabledDomains`
  - ✅ Domain not in either list → disabled
  - ✅ Malformed JSON → throws clear error
  - ✅ Empty `enabledDomains` → all enabled
  - ✅ Case-insensitive domain matching
  - 🔒 Config path not injectable (hardcoded default)
- [ ] `[I]` **Config.cs / ConfigLoader** — Finalize
  - File: `src/Mcpw/Config.cs`
- [ ] `[R]` Review: no file path from user input, read-only after load

### 0.4 Input Validation

- [ ] `[T]` **InputValidatorTests** — (already started in `tests/InputValidatorTests.cs`)
  - ✅ `AssertNoInjection` rejects each injection char
  - ✅ `AssertNoInjection` accepts clean strings
  - ✅ `SanitizePath` blocks `..` traversal
  - ✅ `SanitizePath` blocks `C:\Windows\System32\config`
  - ✅ `SanitizePath` blocks `C:\Windows\NTDS`
  - ✅ `SanitizePath` canonicalizes and returns full path
  - ✅ `SanitizePath(path, allowlist)` blocks paths outside allowlist
  - ✅ `SanitizePath(path, allowlist)` allows paths within allowlist
  - 🔒 Null bytes rejected
  - 🔒 Unicode normalization before comparison
  - 🔒 Trailing dots/spaces stripped (Windows auto-strips)
  - 🔒 ADS (`:hidden`) in paths rejected
  - 🔒 Device paths (`\\.\`) rejected
  - 🔒 UNC paths rejected by default
  - 🔒 Strings > 64KB rejected
  - ✅ `ParsePid` valid integer → returns pid
  - ✅ `ParsePid` non-integer → throws
  - ✅ `ParsePid` negative → throws
- [ ] `[I]` **InputValidator.cs** — Add missing validators: null byte check, Unicode normalization, ADS check, device path check, UNC check, max length check
  - File: `src/Mcpw/InputValidator.cs`
- [ ] `[R]` Review: no regex-based validation where simple char checks suffice, perf-safe

### 0.5 Windows Abstraction Interfaces

- [ ] `[T]` No tests needed — interfaces are contracts, tested via consumers
- [ ] `[I]` **Finalize all interfaces:**
  - `IWmiClient` — `QueryAsync(string wqlQuery)`, `QueryAsync<T>(...)`, typed methods per WMI class
  - `IEventLogAccess` — `ReadAsync(channel, query, count)`, `GetChannels()`, `ClearAsync()`, `Watch()`
  - `IRegistryAccess` — `GetValue()`, `SetValue()`, `DeleteValue()`, `DeleteKey()`, `GetSubKeyNames()`, `GetValueNames()`, `SearchAsync()`
  - `IServiceControl` — `GetServices()`, `GetStatus()`, `Start()`, `Stop()`, `SetStartupType()`, `GetConfig()`
  - `IPowerShellHost` — `InvokeAsync(script, params)` with Constrained Language Mode
  - Files: `src/Mcpw/Windows/I*.cs`
- [ ] `[R]` Review: interfaces are minimal, async-first, no Windows-specific types in signatures (use POCOs)

### 0.6 Windows Abstraction Implementations

- [ ] `[T]` Integration test stubs only (these hit real OS; CI runs mock tests)
- [ ] `[I]` **Implement all concrete classes:**
  - `WmiClient` — `System.Management` ManagementObjectSearcher
  - `EventLogAccess` — `System.Diagnostics.Eventing.Reader`
  - `RegistryAccess` — `Microsoft.Win32.Registry`
  - `ServiceControl` — `System.ServiceProcess.ServiceController`
  - `PowerShellHost` — `System.Management.Automation` (Constrained Language Mode)
  - Files: `src/Mcpw/Windows/*.cs`
- [ ] `[R]` Review: WQL parameterization, no string interpolation, COM cleanup, timeout on all blocking ops

### 0.7 Domain Type POCOs

- [ ] `[T]` Serialization round-trip tests for each type (JSON → object → JSON identity)
- [ ] `[I]` **Complete all Types/**:
  - `SystemTypes.cs` — `SystemInfo`, `UptimeInfo`, `EnvVar`, `SysctlParam`
  - `ProcessTypes.cs` — `ProcessInfo`, `ProcessTree`, `TopResult`
  - `ServiceTypes.cs` — `ServiceInfo`, `ServiceConfig`
  - `LogTypes.cs` — `LogEntry`, `LogChannel`
  - `NetworkTypes.cs` — `NicInfo`, `PortListener`, `Connection`, `FirewallRule`, `Route`, `PingResult`, `TracerouteHop`
  - `FileTypes.cs` — `FileMetadata`, `SearchResult`, `ShareInfo`
  - `IdentityTypes.cs` — `LocalUser`, `LocalGroup`, `WhoAmI`
  - `StorageTypes.cs` — `DiskInfo`, `VolumeInfo`, `SmartData`, `PartitionInfo`
  - `ScheduleTypes.cs` — `ScheduledTask`, `TaskTrigger`, `TaskAction`
  - `SecurityTypes.cs` — `CertInfo`, `AuditPolicy`, `LocalPolicy`, `UpdateStatus`, `DefenderStatus`
  - `ContainerTypes.cs` — `ContainerInfo`, `ContainerImage`, `ExecResult`
  - `HardwareTypes.cs` — `PciDevice`, `UsbDevice`, `BiosInfo`, `MemoryModule`, `GpuInfo`, `BatteryInfo`, `SensorData`, `ThermalZone`, `CpuTemperature`, `FanSpeed`, `VoltageReading`, `CpuDetailInfo`
  - `RegistryTypes.cs` — `RegistryValue`, `RegistryKeyInfo`, `RegistrySearchResult`
  - `IISTypes.cs` — `IisSite`, `AppPool`, `WorkerProcess`, `SiteConfig`
  - `ADTypes.cs` — `AdUser`, `AdGroup`, `AdComputer`, `OuInfo`
  - `HyperVTypes.cs` — `VmInfo`, `VirtualSwitch`, `SnapshotInfo`
  - Files: `src/Mcpw/Types/*.cs`
- [ ] `[R]` Review: `JsonPropertyName` snake_case, nullable annotations, no business logic in types

### 0.8 Daemon (Windows Service Host)

- [ ] `[T]` Test that `ExecuteAsync` calls `server.RunAsync` with Console streams and propagates cancellation
- [ ] `[I]` Already implemented — validate correctness
  - File: `src/Mcpw/Daemon.cs`
- [ ] `[R]` Review: logging levels, graceful shutdown, exception handling

### 0.9 Test Infrastructure

- [ ] `[I]` **Test project setup:**
  - Verify `Mcpw.Tests.csproj` references: xUnit, Moq/NSubstitute, FluentAssertions
  - Create test helpers: `MockWmiClient`, `MockEventLogAccess`, `MockRegistryAccess`, `MockServiceControl`, `MockPowerShellHost`
  - Create `TestFixtures/` folder with JSON fixture data for WMI responses, AD objects, Docker API responses
  - Create `ServerTestHelper` — wraps `McpServer` with `StringReader`/`StringWriter` for easy JSON-RPC round-trip testing
  - File: `tests/Mcpw.Tests/`
- [ ] `[R]` Review: fixtures are realistic, mocks are strict (fail on unexpected calls)

---

## Phase 1: Shared Read-Only Domains (Lowest Risk)

> All 🟢 Read tools, testable with mocks. These have Linux parity and form the core value.  
> **Blocked by:** Phase 0 complete.  
> **All tasks in Phase 1 are parallel with each other.**

### 1.1 `system.*` (6 tools)

- [ ] `[T]` **SystemToolsTests** — (already started in `tests/Tools/SystemToolsTests.cs`)
  - `system.info` (15 tests): hostname, os_name contains "Windows", os_version format, architecture, cpu_cores > 0, cpu_logical >= cpu_cores, memory_total > 0, memory_available <= total, domain, last_boot ISO 8601, timezone, locale, install_date, WMI unavailable partial result, WMI timeout
  - `system.uptime` (8 tests): uptime_seconds > 0, uptime_human format, last_boot ISO 8601, consistency within 5s, multiple calls within 2s consistent, TickCount64 overflow at 49.7d, overflow at 497d, called immediately after boot
  - `system.env` (13 tests): list returns variables, contains PATH, COMPUTERNAME, OS=Windows_NT, count matches length, get specific PATH, COMPUTERNAME, case-insensitive, empty name returns all, invalid target error, nonexistent var, long PATH value, Unicode, empty value
  - `system.env.set` (11 tests): new var previous_value null, overwrite returns previous, persists across restart, correct target, empty name error, name with `=` error, null byte error, invalid target error, protected vars blocked, max name/value length
  - `system.reboot` (10 tests): invalid action error, negative delay error, excessive delay error, correct API call per action, delayed scheduling, force flag, scheduled_at ISO 8601, Dangerous tier required, audit log, already scheduled error
  - `system.sysctl` (7 tests): all returns params, memory category, network category, each param has fields, invalid category error, registry access denied skipped, missing key skipped
- [ ] `[I]` **SystemTools.cs** — Implement all 6 tools using `IWmiClient` + `Environment` APIs
  - File: `src/Mcpw/Tools/SystemTools.cs`
- [ ] `[R]` Self-review checklist

### 1.2 `process.*` (6 tools)

- [ ] `[T]` Create `tests/Tools/ProcessToolsTests.cs`
  - `process.list` (18 tests): non-empty array, System process PID 4, total_count matches, pid > 0, non-empty name, non-negative cpu/memory, sort_by cpu/memory/name/pid, limit:5, filter_name svchost case-insensitive, stale process handle, access denied → nulls, limit:0, limit:100000, filter matches none, filter_user SYSTEM, long command line, orphaned processes
  - `process.inspect` (14 tests): all fields for known PID, modules non-empty, io non-negative, services for svchost, window_title for GUI, non-integer pid error, negative pid error, PID not found, access denied partial, process exits during inspect, PID 0 minimal, PID 4 limited, own process full access, no modules
  - `process.kill` (16 tests): kills test process, correct name, force kills children, non-integer error, negative error, PID not found, PID 0 blocked, PID 4 blocked, csrss blocked, winlogon blocked, lsass blocked, services.exe blocked, parent process blocked, access denied, already exited, audit log
  - `process.top` (12 tests): processes with limit, default 20, sort cpu, sort memory, system_cpu 0-100, system_memory 0-100, memory sums, process_count > 0, thread >= process, handle > 0, limit:1, idle system edge
  - `process.tree` (10 tests): no PID full tree, known parent-child, PID subtree, depth:1 no grandchildren, pid/name/children fields, leaf empty children, PID not found, orphaned at top, concurrent changes partial, deep tree >20, circular ref
  - `process.nice` (8 tests): changes priority, all six levels valid, PID not found error, invalid priority error, realtime requires Operate, protected process blocked, same priority success, process exits between validate/apply
- [ ] `[I]` **ProcessTools.cs** — Implement using `Process` class + `IWmiClient` for extended data
  - File: `src/Mcpw/Tools/ProcessTools.cs`
- [ ] `[R]` Self-review checklist

### 1.3 `service.*` (8 tools)

- [ ] `[T]` **ServiceToolsTests** — (already started in `tests/Tools/ServiceToolsTests.cs`)
  - `service.list` (12 tests): non-empty, Spooler exists, W32Time exists, count matches, status filter running/stopped, type driver, filter_name case-insensitive matches name and display_name, all fields populated, transitional state, no matches, paused filter
  - `service.status` (9 tests): running service, stopped service, extended fields, dependencies, dependent_services, can_stop/can_pause, empty name error, not found error, transitional state, service with spaces
  - `service.start` (12 tests): start stopped → running, previous_status, valid pid, elapsed_ms, empty name error, not found error, negative timeout error, already running error, disabled error, dependencies not running, timeout error, fails to start
  - `service.stop` (11 tests): stop running → stopped, previous_status, force stops dependents, dependents listed, already stopped error, can't stop error, dependents without force error, timeout error, hangs → timeout, critical services blocked, paused service, stop_pending
  - `service.restart` (6 tests): running → stopped → running, both elapsed times, new pid, not running starts it, timeout exceeded, takes long to stop
  - `service.logs` (7 tests): returns entries, lines:10 limit, level error filter, ordered descending, required fields, no entries empty, non-existent service
  - `service.enable` (6 tests): changes startup type, disabled, automatic_delayed, invalid type error, not found error, Operate tier, same type success
  - `service.config` (5 tests): complete config, binary_path, recovery_actions, dependencies, delayed_auto_start, not found error
- [ ] `[I]` **ServiceTools.cs** — Implement using `IServiceControl` + `IWmiClient`
  - File: `src/Mcpw/Tools/ServiceTools.cs`
- [ ] `[R]` Self-review checklist

### 1.4 `log.*` (5 tools)

- [ ] `[T]` **LogToolsTests** — (already started in `tests/Tools/LogToolsTests.cs`)
  - `log.tail` (13 tests): default System returns entries, Application channel, Security channel, lines:5, ordered descending, level error filter, required fields, returned_count matches, negative lines error, lines:0, invalid channel, invalid level, empty channel, million entries efficient
  - `log.search` (13 tests): query "error" matches, source filter, event_id filter, since/until filters, level+query combined, limit caps, since after until error, since future empty, negative limit error, no results empty, invalid XPath internal error, XPath special chars escaped, Unicode query, broad search respects limit
  - `log.stream` (5 tests): starts subscription receives events, level filter, source filter, invalid channel error, channel disabled terminates gracefully
  - `log.channels` (5 tests): non-empty list, contains Application/System/Security, include_empty false, filter Microsoft, no matches
  - `log.clear` (8 tests): clears channel records>0, backup saves .evtx, empty after clear, empty channel name error, backup in blocked dir error, Operate tier, Security log blocked, already empty 0, access denied, backup not writable → channel NOT cleared, clear during writes
- [ ] `[I]` **LogTools.cs** — Implement using `IEventLogAccess`
  - File: `src/Mcpw/Tools/LogTools.cs`
- [ ] `[R]` Self-review checklist

### 1.5 `network.*` (10 tools)

- [ ] `[T]` Create `tests/Tools/NetworkToolsTests.cs`
  - `network.interfaces` (13 tests): non-empty, loopback 127.0.0.1, name/status/type, active has ipv4, MAC format, speed > 0, counters non-negative, VPN adapter, Hyper-V adapter, multi IPv4, IPv6 only, disconnected, DHCP vs static
  - `network.ports` (7 tests): returns listeners, common ports 135/445, TCP only, UDP only, pid > 0, valid port range, IPv6 listeners, same port different IPs
  - `network.connections` (7 tests): returns connections, state established filter, pid filter, port filter, valid state string, time_wait thousands respects limit, limit:1
  - `network.dns` (10 tests): config hostname, dns_servers per interface, search_suffixes, resolve localhost, PTR resolve, public domain, elapsed_ms > 0, nonexistent error, DNS unreachable timeout, CNAME chain, many A records, IPv6 AAAA
  - `network.firewall` (8 tests): firewall_enabled per profile, non-empty rules, direction in, enabled_only true, profile domain, required fields, any rule, IP range, port range, service rule, thousands of rules
  - `network.firewall.add` (8 tests): creates inbound allow, outbound block, specific port/program/profile, missing name/direction/protocol error, invalid port error, duplicate name error, Operate tier, opens all ports blocked, audit log
  - `network.firewall.remove` (3 tests): removes rule, nonexistent error, protected rule error
  - `network.routing` (4 tests): non-empty, default route 0.0.0.0/0, loopback route, valid metric
  - `network.ping` (8 tests): 127.0.0.1 succeeds, count:1, correct sent/received/lost, loss 0%, avg/min/max calculated, unreachable 100% loss, unresolvable error, count:100, IPv6
  - `network.traceroute` (6 tests): 127.0.0.1 1 hop, incrementing hop numbers, reached true, unresolvable error, unreachable reached false, timeout hop \*, max_hops:1
- [ ] `[I]` **NetworkTools.cs** — Implement using `NetworkInterface`, `IPGlobalProperties`, `Ping`, COM `INetFwPolicy2`, `IPowerShellHost` for routing
  - File: `src/Mcpw/Tools/NetworkTools.cs`
- [ ] `[R]` Self-review checklist

### 1.6 `file.*` (13 tools)

- [ ] `[T]` Create `tests/Tools/FileToolsTests.cs`
  - `file.read` (22 tests): UTF-8, UTF-16 LE/BE, auto encoding detection, limit_bytes truncated, offset, size_bytes total, encoding_detected, empty path error, relative path error, negative offset error, negative limit error, path outside allowed error, blocked path SAM error, traversal blocked, UNC blocked, symlink outside blocked, ADS blocked, device path blocked, null byte blocked, trailing dots, not found, locked file, directory path, empty file, binary base64, at limit not truncated, no BOM, long line, Unicode path, MAX_PATH, read-only succeeds
  - `file.write` (16 tests): creates new, overwrites existing, append mode, create_directories, bytes_written, round-trip identical, empty path error, invalid encoding error, invalid mode error, path blocked, system dirs blocked, executable extensions blocked, create_dirs outside allowed blocked, traversal blocked, symlink blocked, disk full, parent not exist, read-only, locked, empty content, large content, concurrent writes
  - `file.info` (14 tests): file fields, directory fields, type file/directory, acl populated, owner format, hidden/readonly/system attributes, symlink is_symlink+target, ADS listed, not found error, access denied, no ADS empty, junction as symlink, root dir, long filename
  - `file.search` (14 tests): pattern \*.log, name_contains, content_contains, recursive true/false, size filters, date filters, type directory, limit truncated, not a directory error, size mismatch error, date mismatch error, path in allowed, content not in blocked, empty dir, permission denied skips, 100k files respects limit, circular symlink, glob not regex
  - `file.mkdir` (5 tests): creates, recursive nested, already exists idempotent, path allowed, parent not exist without recursive
  - `file.delete` (7 tests): deletes file, empty directory, recursive with contents, path allowed, can't delete root prefix, system files blocked, not found, non-empty no recursive, in use, read-only
  - `file.copy` (6 tests): copies file, overwrite replaces, bytes_copied, directory copy, source/dest allowed, source not found, exists no overwrite, insufficient space, same file
  - `file.move` (4 tests): moves file, overwrite, source/dest allowed, cross-volume, exists no overwrite
  - `file.chmod` (5 tests): add allow read, updated ACL, deny rule, invalid rights/type/identity error, path within allowed, system files blocked
  - `file.tail` (6 tests): last 20 default, lines:5, file_size_bytes, fewer lines returns all, empty file, no trailing newline, binary file, very long lines, follow streams
  - `file.share` (3 tests): lists shares including IPC$/ADMIN$
  - `file.share.create` (4 tests): creates share, path in allowed, Operate tier
  - `file.share.remove` (2 tests): removes share, nonexistent error
- [ ] `[I]` **FileTools.cs** — Implement using `System.IO`, `FileSecurity`, `IWmiClient` for shares
  - File: `src/Mcpw/Tools/FileTools.cs`
- [ ] `[R]` Self-review checklist

### 1.7 `identity.*` (5 tools)

- [ ] `[T]` Create `tests/Tools/IdentityToolsTests.cs`
  - `identity.users` (8 tests): non-empty, Administrator/Guest, include_disabled false, filter case-insensitive, name/sid/enabled/groups, last_logon valid, many users, empty full_name, built-in accounts
  - `identity.groups` (4 tests): Administrators/Users/Guests, members array, Administrator in Administrators, member_count matches
  - `identity.whoami` (8 tests): username, is_admin accurate, groups with SIDs, privileges list, SYSTEM → is_system true, integrity_level, LOCAL SERVICE, UAC elevation_type
  - `identity.user.create` (10 tests): creates user, appears in list, groups added, must_change_password, empty username error, invalid chars error, >20 chars error, password complexity error, duplicate error, Operate tier, password not in audit log
  - `identity.user.delete` (5 tests): Dangerous tier, can't delete Administrator, can't delete current user, can't delete mcpw account, profile deletion
- [ ] `[I]` **IdentityTools.cs** — Implement using `IWmiClient` + `DirectoryEntry`
  - File: `src/Mcpw/Tools/IdentityTools.cs`
- [ ] `[R]` Self-review checklist

### 1.8 `storage.*` (5 tools)

- [ ] `[T]` Create `tests/Tools/StorageToolsTests.cs`
  - `storage.disks` (8 tests): at least one disk, size > 0, status OK, partitions > 0, media_type returns HDD/SSD/NVMe correctly, interface returns SATA/NVMe/SAS/USB, firmware_version non-empty, status "Degraded" for failing disk, status "Error" for faulted disk, serial number non-empty

  - `storage.volumes` (7 tests): C: volume, used+free ≈ size, NTFS/ReFS, used_percent 0-100, no drive letter, CD-ROM, BitLocker

  - `storage.usage` (4 tests): all drives, total_gb sum, path specific drive, path not exist error, network drive

  - `storage.smart` (14 tests): returns SMART data for all disks, status "OK"/"Caution"/"Bad" reflects health, `temperature_celsius` returns disk temp (non-null for physical disks), `temperature_celsius` null for virtual disks, `power_on_hours` returns sensible value > 0, `reallocated_sectors` returns count (0 for healthy disk), `pending_sectors` returns count, `attributes` array contains standard SMART attributes (id, name, value, worst, threshold, raw), specific disk index filter works, SSD/NVMe returns NVMe health info (wear leveling, available spare), HDD returns traditional SMART (spin retry, seek error rate), USB drive returns error or empty attributes, virtual disk (Hyper-V/VMware) → clear error "SMART not available", `status: "Caution"` when reallocated_sectors > threshold

  - `storage.partitions` (4 tests): system disk ≥1 partition, EFI on UEFI, GPT/MBR, sizes sum ≈ disk
- [ ] `[I]` **StorageTools.cs** — Implement using `DriveInfo` + `IWmiClient`
  - File: `src/Mcpw/Tools/StorageTools.cs`
- [ ] `[R]` Self-review checklist

### 1.9 `schedule.*` (6 tools)

- [ ] `[T]` Create `tests/Tools/ScheduleToolsTests.cs`
  - `schedule.list` (7 tests): returns tasks, include_microsoft, exclude_microsoft, include_disabled, root folder, recursive subfolders, required fields, empty folder, multi trigger, running status
  - `schedule.info` (3 tests): full definition, triggers/conditions/settings/history, not found error
  - `schedule.add` (10 tests): daily task, weekly with days, boot trigger, logon trigger, next_run populated, empty name error, invalid trigger error, daily without start_time error, weekly without days error, nonexistent command, invalid run_level, Operate tier, SYSTEM without admin, allowed executable paths, shell command blocked
  - `schedule.remove` (3 tests): removes task, not found error, currently running
  - `schedule.run` (3 tests): triggers execution, status running, disabled error, already running
  - `schedule.enable` (3 tests): disable → enabled false, enable → enabled true, previous_enabled correct
- [ ] `[I]` **ScheduleTools.cs** — Implement using COM `ITaskService` via `IPowerShellHost` or interop
  - File: `src/Mcpw/Tools/ScheduleTools.cs`
- [ ] `[R]` Self-review checklist

### 1.10 `security.*` (5 tools)

- [ ] `[T]` Create `tests/Tools/SecurityToolsTests.cs`
  - `security.certs` (9 tests): root CAs, personal certs, machine vs user, thumbprint/subject/not_before/not_after, is_expired, days_until_expiry, expiring_within_days, self-signed, no SAN, wildcard, expired cert, empty store
  - `security.audit_policy` (3 tests): non-empty categories, Account Logon/Logon-Logoff/Object Access, success/failure booleans
  - `security.local_policy` (3 tests): password_policy with min_length/max_age, user_rights array, complexity_required boolean
  - `security.windows_update` (6 tests): last_check/last_install timestamps, reboot_required boolean, pending_updates, installed_updates limit, WU service not running, WSUS unreachable, no updates ever, pending_only
  - `security.defender` (5 tests): Defender status, antivirus_enabled, definition_date recent, recent_threats, Defender not installed, Defender service not running
- [ ] `[I]` **SecurityTools.cs** — Implement using `X509Store`, `IPowerShellHost` for audit/local policy, `IWmiClient` for Defender
  - File: `src/Mcpw/Tools/SecurityTools.cs`
- [ ] `[R]` Self-review checklist

### 1.11 `container.*` (8 tools)

- [ ] `[T]` Create `tests/Tools/ContainerToolsTests.cs`
  - `container.list` (12 tests): returns containers, all false running only, all true includes stopped, filter_name, filter_image, filter_status running, id/name/image/state, ports, count matches, Docker not installed, daemon not running, named pipe not accessible, no ports, many labels, restarting state, combined filters AND
  - `container.inspect` (8 tests): full details by ID, by name, environment, health_check, platform, not found error, partial ID unique, ambiguous partial, no health check null, restart_policy, resource_limits
  - `container.logs` (7 tests): stdout, tail:10, since filter, stderr:false, stdout:false, not found error, no logs, large logs respects tail, binary output, tail:0
  - `container.exec` (8 tests): simple command, failed command exit_code, user param, working_dir, env inject, empty command error, not found error, not running error, command not found, timeout, Operate tier, audit log, no TTY
  - `container.start/stop/restart` (8 tests): start → running, stop → exited, restart → running new uptime, already running error, already stopped error, stop timeout force kill, not found error, Operate tier
  - `container.images` (4 tests): returns images, id/repository/tag/size, filter_name, dangling, Docker not running, no images
- [ ] `[I]` **ContainerTools.cs** — Implement using Docker Engine API over named pipe `//./pipe/docker_engine`
  - File: `src/Mcpw/Tools/ContainerTools.cs`
- [ ] `[R]` Self-review checklist

### 1.12 `hardware.*` (8 tools)


- [ ] `[T]` Create `tests/Tools/HardwareToolsTests.cs`

  - `hardware.pci` (7 tests): non-empty, display adapter, storage controller, class display filter, class network filter, name/device_id/status, driver_version, no driver null, disabled device, VM few devices

  - `hardware.usb` (4 tests): returns list, name/device_id, class type, no USB empty, hub, no manufacturer

  - `hardware.bios` (7 tests): bios_vendor, bios_version, UEFI/Legacy, secure_boot, motherboard, system_serial, uuid, VM vendor, "To Be Filled", Legacy secure_boot false

  - `hardware.memory` (5 tests): at least one module, total_capacity > 0, used <= total, speeds, VM single module, empty slots

  - `hardware.gpu` (7 tests): at least one adapter, name non-empty, vram > 0 discrete, resolution WxH, driver_version, Server Core, RDP, multiple GPUs, VM, `gpu_temperature_celsius` returned (null if unsupported), `gpu_usage_percent` for discrete GPUs (null if unavailable)

  - `hardware.battery` (7 tests): laptop battery charge_percent, on_ac_power, health_percent, desktop no battery empty, fully charged, critically low, `estimated_runtime_minutes` > 0 when discharging, `cycle_count` returned (null if unsupported), `design_capacity_mwh` vs `full_charge_capacity_mwh` → health_percent accuracy

  - `hardware.sensors` 🟢 Read (12 tests) — **NEW TOOL**: Exposes thermal zones, CPU temperature, fan speeds, and voltage readings

    - ✅ Returns `thermal_zones` array with at least one zone on physical hardware

    - ✅ Each zone has `name`, `temperature_celsius`, `critical_celsius`, `throttle_celsius`

    - ✅ `cpu_temperatures` array returns per-core or per-package temps (null on unsupported hardware)

    - ✅ `cpu_temperatures[].celsius` is plausible range (15–105°C)

    - ✅ `fan_speeds` array returns fan RPM values (empty on fanless/VM)

    - ✅ Each fan has `name` and `rpm` (0 = stopped, null = not readable)

    - ✅ `voltages` array returns voltage readings (empty on unsupported)

    - ✅ Each voltage has `name` (e.g., "CPU Vcore", "3.3V", "12V") and `volts`

    - ✅ Virtual machine → `thermal_zones` may have one zone, `fan_speeds` empty, `cpu_temperatures` null

    - ✅ Server hardware → multiple zones, multiple fans

    - ⚡ Sensor not readable → null value, not error

    - ⚡ IPMI/BMC available → richer data (via `IPowerShellHost` Get-PcsvDevice)

    - **Implementation:** WMI `MSAcpi_ThermalZoneTemperature` (root/WMI), `Win32_Fan`, `Win32_TemperatureProbe`, `Win32_VoltageProbe`, fallback to `IPowerShellHost` for `Get-PcsvDevice` (IPMI/BMC)

  - `hardware.cpu` 🟢 Read (10 tests) — **NEW TOOL**: CPU detailed status including frequency, load, and throttling

    - ✅ Returns `processors` array with at least one entry

    - ✅ Each processor has `name`, `manufacturer`, `max_clock_mhz`, `current_clock_mhz`

    - ✅ `current_clock_mhz` ≤ `max_clock_mhz` (may equal on desktop, lower on laptop power-save)

    - ✅ `load_percent` between 0–100 reflecting current CPU utilization

    - ✅ `cores` and `logical_processors` count populated

    - ✅ `architecture` is "x64" or "ARM64"

    - ✅ `socket` identifies physical socket (e.g., "LGA 1700")

    - ✅ `l2_cache_kb` and `l3_cache_kb` populated

    - ✅ `throttled` boolean — true if CPU is power-throttled below max frequency

    - ✅ `voltage_volts` returns CPU voltage (null if unsupported)

    - ⚡ Virtual machine → `current_clock_mhz` may not reflect real frequency

    - ⚡ Multi-socket server → multiple entries in `processors` array

    - **Implementation:** WMI `Win32_Processor` + `Win32_PerfFormattedData_Counters_ProcessorInformation` for real-time frequency + `Win32_PerfFormattedData_PerfOS_Processor` for load

- [ ] `[I]` **HardwareTools.cs** — Implement using `IWmiClient` (`Win32_PnPEntity`, `Win32_BIOS`, `Win32_PhysicalMemory`, `Win32_VideoController`, `Win32_Battery`, `MSAcpi_ThermalZoneTemperature`, `Win32_Processor`, `Win32_Fan`, `Win32_TemperatureProbe`, `Win32_VoltageProbe`)
  - File: `src/Mcpw/Tools/HardwareTools.cs`
  - **New types needed in `HardwareTypes.cs`**: `SensorData` (thermal_zones, cpu_temperatures, fan_speeds, voltages), `CpuDetailInfo` (name, max/current clock, load, cores, throttled, voltage, cache sizes, socket)

- [ ] `[R]` Self-review checklist — Extra attention: many WMI sensor classes return empty on VMs or unsupported hardware; every field must gracefully return null, never crash

### 1.13 `time.*` (3 tools)

- [ ] `[T]` Create `tests/Tools/TimeToolsTests.cs`
  - `time.info` (8 tests): local_time/utc_time ISO 8601, local-offset ≈ utc within 2s, timezone_id valid, daylight_saving, ntp_server, source ntp/domain, UTC offset +00:00, half-hour offset, DST transition, domain-joined source
  - `time.sync` (4 tests): forces sync synced true, offset_ms, source NTP server, NTP unreachable error, W32Time not running error, Operate tier
  - `time.set_timezone` (4 tests): changes timezone returns previous/new, local_time reflects new, invalid timezone error, Operate tier
- [ ] `[I]` **TimeTools.cs** — Implement using `TimeZoneInfo`, `IPowerShellHost` for w32tm/tzutil
  - Note: Need to add `TimeTools.cs` to `src/Mcpw/Tools/` and `TimeTypes.cs` to `src/Mcpw/Types/`
  - Register in `Program.cs` as `RegisterIfEnabled<TimeTools>(services, config, "time")`
- [ ] `[R]` Self-review checklist

---

## Phase 2: Windows-Specific Read-Only Domains

> **Blocked by:** Phase 0 complete (can run parallel with Phase 1).

### 2.1 `registry.*` (6 tools)

- [ ] `[T]` **RegistryToolsTests** — (already started in `tests/Tools/RegistryToolsTests.cs`)
  - `registry.get` (16 tests): REG_SZ, REG_DWORD, REG_QWORD, REG_MULTI_SZ, REG_EXPAND_SZ, REG_BINARY hex, default value, known key CurrentVersion/ProductName, type field, empty key error, invalid root error, malformed path, key not found, value not found, access denied, blocked SAM, blocked SECURITY, blocked Lsa\Secrets, traversal, large binary, empty MULTI_SZ, empty default, no values, HKCU as SYSTEM
  - `registry.set` (14 tests): creates REG_SZ, REG_DWORD, overwrites returns previous, create_key true, new value null previous, invalid type error, data type mismatch, DWORD overflow, QWORD overflow, Dangerous tier, blocked keys, boot-critical blocked, audit log, autostart Run key blocked, key not exist create_key false, access denied, virtual registry read-only, empty MULTI_SZ, EXPAND_SZ with %SystemRoot%, concurrent writes
  - `registry.delete` (9 tests): deletes value, deletes key recursive, subkeys count, no recursive with subkeys error, Dangerous tier, can't delete root hives, can't delete CurrentControlSet, audit log, not found, access denied
  - `registry.list` (6 tests): lists subkeys/values, depth:1, depth:2, subkey/value counts, name/type/data, not found error, access denied skips
  - `registry.search` (9 tests): find key name, value name, data content, search_in keys/data, limit caps, max_depth, no matches empty, access denied skips, blocked keys excluded, large hive respects limit, regex chars literal, binary hex match
  - `registry.export` (4 tests): valid .reg format, re-importable, includes all recursive, counts correct, blocked keys can't export
- [ ] `[I]` **RegistryTools.cs** — Implement using `IRegistryAccess`
  - File: `src/Mcpw/Tools/RegistryTools.cs`
- [ ] `[R]` Self-review checklist

### 2.2 `iis.*` (11 tools)

- [ ] `[T]` Create `tests/Tools/IISToolsTests.cs`
  - `iis.sites` (4 tests): returns sites, Default Web Site, bindings/physical_path/pool, state started, IIS not installed error
  - `iis.pools` (4 tests): returns pools, DefaultAppPool, runtime_version/pipeline_mode/state, worker_processes count
  - `iis.site.start/stop` (6 tests): stop → stopped, start → started, already started error, already stopped error, port conflict, not found error, Operate tier
  - `iis.pool.start/stop/recycle` (6 tests): stop terminates workers, start available, recycle new PID, recycle stopped error, not found error
  - `iis.site.config` (4 tests): complete config, bindings, authentication, default_documents, logging, not found error
  - `iis.site.config.set` (6 tests): add binding, change physical_path, enable/disable auth, invalid setting error, invalid binding error, path not exist error, Operate tier, path in allowed, ports <1024 policy
  - `iis.pool.config/config.set` (5 tests): detailed config, change runtime, change identity, change recycle interval, No Managed Code, custom identity
  - `iis.worker_processes` (3 tests): active workers, PID matches w3wp, pool_name correct, no workers empty, web garden multiple
- [ ] `[I]` **IISTools.cs** — Implement using `Microsoft.Web.Administration.ServerManager` (or `IPowerShellHost` fallback)
  - File: `src/Mcpw/Tools/IISTools.cs`
- [ ] `[R]` Self-review checklist

### 2.3 `printer.*` (16 tools)

- [ ] `[T]` Create `tests/Tools/PrinterToolsTests.cs`
  - `printer.list` (10 tests): returns printers, include_network/local, default_printer, name/status/driver_name, is_default one printer, jobs_count, type local/network/virtual, no printers, Spooler not running, PDF printer virtual, error status, offline
  - `printer.info` (6 tests): complete info, paper_sizes, resolutions, total_pages/jobs, port_name, not found error, case-insensitive, offline partial
  - `printer.jobs` (8 tests): returns jobs, all printers if no name, status pending/printing/error/paused, document/owner/submitted/size, ordered by position, total_count, no jobs empty, not found error, deleting transient, 0 total_pages, large job, network owner domain
  - `printer.job.cancel` (5 tests): cancels pending, returns document, not found error, completed error, different user denied, currently printing, Operate tier, audit log
  - `printer.job.pause/resume` (4 tests): pause holds, resume prints, already paused, not paused, not found error
  - `printer.queue.clear` (5 tests): cancels all, count returned, empty after, empty queue 0, not found error, Operate tier, active printing, 100+ jobs, different users
  - `printer.spooler.status` (4 tests): running status + pid, spool_directory, spool_size_mb, temp_files_count, stopped null pid
  - `printer.spooler.restart` (4 tests): restarts success, clear_queue spool_files_cleared, no clear preserves jobs, elapsed_ms, fails error, locked files
  - `printer.spooler.clear` (4 tests): stops/deletes/restarts, files_deleted + bytes_freed, empty spool 0, corrupted files
  - `printer.pause/resume` (4 tests): pause → paused, resume → printing, pending_jobs, already paused, not found
  - `printer.test` (3 tests): test_page_sent, job_id, appears in jobs, offline error, virtual printer
  - `printer.set_default` (3 tests): changes default, previous_default, not found error, already default
  - `printer.drivers` (3 tests): returns drivers, name/version/architecture, used_by, no printers using, x86+x64
  - `printer.ports` (3 tests): returns ports, TCP address/port_number, USB type, used_by, WSD
- [ ] `[I]` **PrinterTools.cs** — Implement using `IWmiClient` (`Win32_Printer`, `Win32_PrintJob`, `Win32_PrinterDriver`, `Win32_TCPIPPrinterPort`)
  - Note: Need to add `PrinterTools.cs` to `src/Mcpw/Tools/` and `PrinterTypes.cs` to `src/Mcpw/Types/`
  - Register in `Program.cs` as `RegisterIfEnabled<PrinterTools>(services, config, "printer")`
- [ ] `[R]` Self-review checklist

---

## Phase 3: Mutating/Operate-Tier Tools

> These modify system state. Extra caution and audit logging required.  
> **Blocked by:** Phase 1 read-only tools for the same domain must be complete first (e.g., `service.start` needs `service.list` working).

### 3.1 Operate-tier tools within existing domains

> Already covered in Phase 1-2 tool lists above. The tasks are grouped per domain, but the TDD cycle should implement read-only tools first, then mutating tools within each domain.

Implementation order within each domain:

1. All 🟢 Read tools (tests → implement)
2. All 🟡 Operate tools (tests → implement)
3. All 🔴 Dangerous tools (tests → implement, extra security review)

### 3.2 Audit Trail Infrastructure

- [ ] `[T]` Create `tests/AuditTrailTests.cs`
  - ✅ Mutating tool call creates audit entry
  - ✅ Audit entry includes: timestamp, tool name, parameters (redacted secrets), result, user
  - ✅ Passwords/secrets never appear in audit entries
  - 🔒 Audit log cannot be disabled by config
  - 🔒 Audit log file in protected directory
- [ ] `[I]` **AuditTrail.cs** — Central audit logger for all Operate/Domain/Dangerous operations
  - File: `src/Mcpw/AuditTrail.cs`
- [ ] `[R]` Review: no sensitive data in logs, structured logging format

### 3.3 Privilege Tier Enforcement

- [ ] `[T]` Create `tests/PrivilegeTierTests.cs`
  - ✅ Read tool accessible at all tiers
  - ✅ Operate tool blocked below Operate tier
  - ✅ Domain tool blocked below Domain tier
  - ✅ Dangerous tool blocked below Dangerous tier
  - ✅ Tier from config correctly enforced
  - 🔒 Insufficient tier → clear error, not crash
- [ ] `[I]` **Privilege tier check** — Add `[PrivilegeTier("operate")]` attribute or tier validation in `IToolHandler.CallAsync` dispatch
- [ ] `[R]` Review: defense in depth — both at server routing and tool entry point

---

## Phase 4: Domain-Tier Tools (AD, Hyper-V, GPO)

> Require domain credentials and domain-joined machine. Integration tests need a test domain or AD LDS.  
> **Blocked by:** Phase 0 + Privilege Tier from Phase 3.

### 4.1 `ad.*` (11 tools)

- [ ] `[T]` Create `tests/Tools/ADToolsTests.cs`
  - `ad.users` (12 tests): returns users, filter by name/SAM/email, OU restriction, enabled_only, limit, all required fields, member_of, not domain-joined error, DC unreachable, invalid OU DN, access denied, LDAP injection sanitized, can't query password attrs, injection attempt escaped, 10k users paged, Unicode, empty fields null, properties custom attrs
  - `ad.groups` (6 tests): returns groups, type security/distribution, scope global/universal, member_count, LDAP injection, 1000+ members ranged retrieval, built-in groups
  - `ad.user.info` (9 tests): by SAM, by UPN, by DN, all_groups recursive, uac_flags decoded, sid format, not found error, ambiguous error, many groups, account_expires, locked_out, no manager null
  - `ad.user.groups` (5 tests): recursive true nested, recursive false direct, direct flag, count matches, circular nesting handled, primary group, deep nesting >10
  - `ad.group.members` (5 tests): returns members, recursive members, user/group/computer types, not found error, empty group, 1500+ ranged retrieval, foreign security principal
  - `ad.computers` (5 tests): returns computers, os_filter, stale_days, dns_hostname/os, no last logon, disabled, no OS info
  - `ad.ou.list` (4 tests): OU tree, root parameter, depth:1, child counts, gpo_links
  - `ad.user.enable/disable` (6 tests): enable disabled → true, disable enabled → false, previously_enabled, not found, access denied, already in state, can't disable Domain Admin, can't disable self, audit log
  - `ad.user.unlock` (3 tests): unlocks locked, not locked → was_locked false, not found error
  - `ad.user.resetpw` (8 tests): resets password, must_change flag, empty password error, complexity error, Dangerous tier, password NOT in audit log, higher-privilege blocked, rate limited, Unicode password, very long password, cannot change flag still works
- [ ] `[I]` **ADTools.cs** — Implement using `System.DirectoryServices.Protocols` for LDAP, paged results, ranged member retrieval
  - File: `src/Mcpw/Tools/ADTools.cs`
- [ ] `[R]` LDAP injection review: all filter inputs escaped with `EscapeLdapFilterValue()`

### 4.2 `hyperv.*` (6 tools)

- [ ] `[T]` Create `tests/Tools/HyperVToolsTests.cs`
  - `hyperv.vms` (6 tests): returns VMs, state running/off filter, required fields, running VM memory > 0, Hyper-V not installed error, not enabled error, no VMs empty
  - `hyperv.vm.info` (10 tests): full details by name, by GUID, disks array, network_adapters, running cpu_usage, guest_os, not found error, no disks, no network, Gen 1 vs Gen 2, multiple disks, ISO mounted, snapshots, dynamic memory
  - `hyperv.vm.start` (5 tests): starts VM → running, already running error, saved state resumes, missing VHD error, insufficient resources
  - `hyperv.vm.stop` (5 tests): force false graceful, force true turn off, already off error, no IC → timeout suggest force, paused can stop
  - `hyperv.vm.restart` (3 tests): force false graceful, force true reset, VM off error
  - `hyperv.vm.snapshot` (5 tests): creates checkpoint, custom name, auto-generated name with timestamp, appears in info, insufficient disk, not found, running vs off
  - `hyperv.switches` (3 tests): lists switches, type/interface, connected_vms, no switches, default switch, external NIC team
- [ ] `[I]` **HyperVTools.cs** — Implement using `IWmiClient` against `root/virtualization/v2` namespace (`Msvm_*`)
  - File: `src/Mcpw/Tools/HyperVTools.cs`
- [ ] `[R]` Self-review checklist

### 4.3 `gpo.*` (3 tools)

- [ ] `[T]` Create `tests/Tools/GPOToolsTests.cs`
  - `gpo.list` (6 tests): returns GPOs, target computer/user, name/guid/status, link_order, not domain-joined error, no GPOs empty, enforced flag, WMI filter, denied status
  - `gpo.result` (5 tests): RSoP returned, computer_settings grouped, user_settings grouped, security_groups, format full/summary, RSOP fails error, many GPOs >20
  - `gpo.update` (4 tests): target both refreshes both, force reapplies all, not domain-joined error, DC unreachable error, Operate tier
- [ ] `[I]` **GPOTools.cs** — Implement using `IPowerShellHost` (`gpresult`, `gpupdate`) or WMI RSOP namespace
  - File: `src/Mcpw/Tools/GPOTools.cs`
- [ ] `[R]` Self-review checklist

---

## Phase 5: Cross-Cutting Concerns & Integration

> **Blocked by:** Phases 1-4 for all relevant domains.

### 5.1 Global Test Suite

- [ ] `[T]` Create `tests/GlobalTests.cs` — Tests from specs/tests.md "Global Tests" section applied to ALL tools:
  - ✅ Valid JSON-RPC 2.0 response for every tool call (parameterized test over all enabled tools)
  - ✅ Error code -32601 for disabled domains
  - ✅ Error code -32602 for missing required params
  - ✅ Error code -32602 for wrong param types
  - ✅ Error code -32603 for unexpected failures, no stack trace
  - ✅ Tool name case-sensitive matching
  - ✅ Extra params ignored
  - ✅ Response includes all documented required fields
  - ✅ Response field types match documentation
  - ✅ Null/optional present as null, not omitted
  - ✅ Timestamps ISO 8601 UTC
  - ✅ Tool manifest includes all enabled tools with correct schemas
  - 🔒 No stack traces in errors
  - 🔒 No raw .NET exception messages
  - 🔒 No data outside allowed paths
  - 🔒 Privilege tier enforced
  - 🔒 Injection chars rejected in all string inputs
  - 🔒 Unicode normalization before path validation
  - 🔒 Null bytes rejected
  - 🔒 Strings > 64KB rejected
  - 🔒 Concurrent calls no race conditions

### 5.2 Performance Benchmarks

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

### 5.3 Integration Tests

- [ ] `[T]` Create `tests/Integration/` folder — (mark as `[Category("Integration")]`):
  - 🧪 MCP Inspector end-to-end via stdio
  - 🧪 Full `initialize` → `tools/list` → `tools/call` round-trip
  - 🧪 Service start/stop using dedicated `mcpw-test-svc`
  - 🧪 File I/O in temp directory within allowed paths
  - 🧪 Process kill of test child processes
  - 🧪 Docker operations with test containers (if Docker available)
  - 🧪 Printer operations with virtual PDF printer
  - 🧪 Event log write + read back

### 5.4 Test Service for service.\* Integration Tests

- [ ] `[I]` Create `mcpw-test-svc` — Minimal Windows Service for testing:
  - Installs via `sc.exe create mcpw-test-svc`
  - Supports start/stop/pause
  - Logs to known Event Log source
  - Configurable startup behavior (fast, slow, crash-on-start)
  - Auto-cleanup in test teardown

### 5.5 Deployment Validation

- [ ] `[T]` Test `deploy/install.ps1`:
  - Installs as Windows Service
  - Service starts successfully
  - Service responds to `initialize` over stdio
  - Service stops cleanly
  - Service uninstalls cleanly
- [ ] `[I]` Finalize `deploy/install.ps1`

---

## Phase 6: Polish & Finalize

> **Blocked by:** All phases complete.

### 6.1 Documentation

- [ ] Update `README.md` with final tool counts and any changes
- [ ] Create `docs/tools.md` — auto-generated from `tools/list` output
- [ ] Validate all tool `inputSchema` in manifest matches specs/features.md

### 6.2 Final Review

- [ ] Run full test suite: `dotnet test` — all pass
- [ ] Run `dotnet test --filter "Category=Integration"` on a Windows machine with all features
- [ ] Run MCP Inspector manually against published binary
- [ ] Code coverage report — target >90% for tool handlers
- [ ] Security review: grep for `string.Format`, `$"..."` in WQL/LDAP/XPath/shell contexts
- [ ] Performance review: no sync-over-async, no unbounded allocations

### 6.3 CI/CD Pipeline

- [ ] Configure GitHub Actions (or Azure DevOps) for:
  - Build: `dotnet build`
  - Unit tests: `dotnet test --filter "Category!=Integration&Category!=Performance"`
  - Publish: `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
  - Nightly: integration + performance tests on Windows runner

---

## Summary

| Phase     | Scope              | Domains | Tools   | Tests (approx) | Dependencies |
| --------- | ------------------ | ------- | ------- | -------------- | ------------ |
| **0**     | Infrastructure     | —       | —       | ~50            | None         |
| **1**     | Shared Read+Write  | 13      | 98      | ~950           | Phase 0      |
| **2**     | Windows-Specific   | 3       | 33      | ~200           | Phase 0      |
| **3**     | Audit + Privileges | —       | —       | ~20            | Phase 0      |
| **4**     | Domain-Tier        | 3       | 20      | ~120           | Phase 0+3    |
| **5**     | Integration        | —       | All     | ~70            | Phases 1-4   |
| **6**     | Polish             | —       | —       | —              | All          |
| **Total** |                    | **19**  | **141** | **~1,360**     |              |
