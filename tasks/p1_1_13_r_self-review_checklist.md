# Task: `[R]` Self-review checklist

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.13 `time.*` (3 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` Self-review checklist

## Tool Specifications

### Feature: time.*
## 13. `time.*` — Time and NTP

### Test Spec: time.*
## 13. `time.*`

### Feature: time.* — Time and NTP
## 13. `time.*` — Time and NTP

### Feature: time.info
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

### Test Spec: time.info
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

### Feature: time.set_timezone
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

### Test Spec: time.set_timezone
### `time.set_timezone`

**Happy Path:**

- 🎭 Changes timezone → returns previous and new
- 🎭 `local_time` in response reflects new timezone

**Input Validation:**

- ✅ Invalid timezone ID → error "Timezone not found"

**Security:**

- 🔒 Requires Operate tier

---

### Feature: time.sync
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

### Test Spec: time.sync
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

