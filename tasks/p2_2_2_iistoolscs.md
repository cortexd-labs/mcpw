# Task: IISTools.cs

**Phase 2: Windows-Specific Read-Only Domains**
**Sub-phase: 2.2 `iis.*` (11 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **IISTools.cs** — Implement using `Microsoft.Web.Administration.ServerManager` (or `IPowerShellHost` fallback)
  - File: `src/Mcpw/Tools/IISTools.cs`

## Tool Specifications

### Feature: iis.*
## 15. `iis.*` — IIS Web Server

### Test Spec: iis.*
## 15. `iis.*`

### Feature: iis.* — IIS Web Server
## 15. `iis.*` — IIS Web Server

### Feature: iis.pool.config
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

### Test Spec: iis.pool.config
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

### Feature: iis.pool.config.set
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

### Feature: iis.pool.recycle
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

### Feature: iis.pool.start
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

### Test Spec: iis.pool.start
### `iis.pool.start` / `iis.pool.stop` / `iis.pool.recycle`

**Happy Path:**

- 🎭 Stop pool → worker processes terminate
- 🎭 Start pool → pool available
- 🎭 Recycle → `previous_pid` differs from new worker PID

**Error Handling:**

- ✅ Recycle stopped pool → error
- ✅ Non-existent pool → error

---

### Feature: iis.pool.stop
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

### Feature: iis.pools
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

### Test Spec: iis.pools
### `iis.pools`

**Happy Path:**

- ✅ Returns list of application pools
- ✅ DefaultAppPool exists on fresh IIS
- ✅ Each pool has `runtime_version`, `pipeline_mode`, `state`
- ✅ `worker_processes` count is accurate

---

### Feature: iis.site.config
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

### Test Spec: iis.site.config
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

### Feature: iis.site.config.set
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

### Test Spec: iis.site.config.set
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

### Feature: iis.site.start
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

### Test Spec: iis.site.start
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

### Feature: iis.site.stop
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

### Feature: iis.sites
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

### Test Spec: iis.sites
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

### Feature: iis.worker_processes
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

### Test Spec: iis.worker_processes
### `iis.worker_processes`

**Happy Path:**

- ✅ Returns active worker processes for running pools
- ✅ PID matches actual w3wp.exe process
- ✅ `pool_name` correctly identifies the pool

**Edge Cases:**

- ⚡ No active workers (all pools idle) → empty array
- ⚡ Pool with multiple workers (web garden) → all listed

---

