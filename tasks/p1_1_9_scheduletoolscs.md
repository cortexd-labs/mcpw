# Task: ScheduleTools.cs

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.9 `schedule.*` (6 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **ScheduleTools.cs** — Implement using COM `ITaskService` via `IPowerShellHost` or interop
  - File: `src/Mcpw/Tools/ScheduleTools.cs`

## Tool Specifications

### Feature: schedule.*
## 9. `schedule.*` — Scheduled Tasks

### Test Spec: schedule.*
## 9. `schedule.*`

### Feature: schedule.* — Scheduled Tasks
## 9. `schedule.*` — Scheduled Tasks

### Feature: schedule.add
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

### Test Spec: schedule.add
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

### Feature: schedule.enable
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

### Test Spec: schedule.enable
### `schedule.enable`

**Happy Path:**

- 🎭 Disabling enabled task → `enabled: false`
- 🎭 Enabling disabled task → `enabled: true`
- 🎭 Returns `previous_enabled` correctly

---

### Feature: schedule.info
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

### Test Spec: schedule.info
### `schedule.info`

**Happy Path:**

- ✅ Returns full task definition for valid path
- ✅ Includes triggers, conditions, settings, history count
- ✅ `registration_date` is valid timestamp

**Error Handling:**

- ✅ Invalid task path → error "Task not found"

---

### Feature: schedule.list
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

### Test Spec: schedule.list
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

### Feature: schedule.remove
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

### Test Spec: schedule.remove
### `schedule.remove`

**Happy Path:**

- 🎭 Removes task → no longer in list

**Error Handling:**

- ✅ Non-existent path → error
- ✅ Currently running task → remove still works (task finishes current run)

---

### Feature: schedule.run
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

### Test Spec: schedule.run
### `schedule.run`

**Happy Path:**

- 🎭 Triggers immediate execution of scheduled task
- 🎭 Task `status` changes to "running"

**Error Handling:**

- ✅ Disabled task → error
- ✅ Task already running → error or starts new instance (depends on settings)

---

