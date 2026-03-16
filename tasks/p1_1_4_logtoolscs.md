# Task: LogTools.cs

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.4 `log.*` (5 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **LogTools.cs** — Implement using `IEventLogAccess`
  - File: `src/Mcpw/Tools/LogTools.cs`

## Tool Specifications

### Feature: log.*
## 4. `log.*` — Log Access

### Test Spec: log.*
## 4. `log.*`

### Feature: log.* — Log Access
## 4. `log.*` — Log Access

### Feature: log.channels
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

### Test Spec: log.channels
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

### Feature: log.clear
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

### Test Spec: log.clear
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

### Feature: log.search
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

### Test Spec: log.search
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

### Feature: log.stream
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

### Test Spec: log.stream
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

