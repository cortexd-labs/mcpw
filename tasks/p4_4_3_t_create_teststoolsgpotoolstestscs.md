# Task: `[T]` Create `tests/Tools/GPOToolsTests.cs`

**Phase 4: Domain-Tier Tools (AD, Hyper-V, GPO)**
**Sub-phase: 4.3 `gpo.*` (3 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` Create `tests/Tools/GPOToolsTests.cs`
  - `gpo.list` (6 tests): returns GPOs, target computer/user, name/guid/status, link_order, not domain-joined error, no GPOs empty, enforced flag, WMI filter, denied status
  - `gpo.result` (5 tests): RSoP returned, computer_settings grouped, user_settings grouped, security_groups, format full/summary, RSOP fails error, many GPOs >20
  - `gpo.update` (4 tests): target both refreshes both, force reapplies all, not domain-joined error, DC unreachable error, Operate tier

## Tool Specifications

### Feature: gpo.*
## 18. `gpo.*` — Group Policy

### Test Spec: gpo.*
## 18. `gpo.*`

### Feature: gpo.* — Group Policy
## 18. `gpo.*` — Group Policy

### Feature: gpo.list
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

### Test Spec: gpo.list
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

### Feature: gpo.result
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

### Test Spec: gpo.result
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

### Feature: gpo.update
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

### Test Spec: gpo.update
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

