# Task: `[T]` Create `tests/Tools/IdentityToolsTests.cs`

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.7 `identity.*` (5 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` Create `tests/Tools/IdentityToolsTests.cs`
  - `identity.users` (8 tests): non-empty, Administrator/Guest, include_disabled false, filter case-insensitive, name/sid/enabled/groups, last_logon valid, many users, empty full_name, built-in accounts
  - `identity.groups` (4 tests): Administrators/Users/Guests, members array, Administrator in Administrators, member_count matches
  - `identity.whoami` (8 tests): username, is_admin accurate, groups with SIDs, privileges list, SYSTEM → is_system true, integrity_level, LOCAL SERVICE, UAC elevation_type
  - `identity.user.create` (10 tests): creates user, appears in list, groups added, must_change_password, empty username error, invalid chars error, >20 chars error, password complexity error, duplicate error, Operate tier, password not in audit log
  - `identity.user.delete` (5 tests): Dangerous tier, can't delete Administrator, can't delete current user, can't delete mcpw account, profile deletion

## Tool Specifications

### Feature: identity.*
## 7. `identity.*` — User and Group Info

### Test Spec: identity.*
## 7. `identity.*`

### Feature: identity.* — User and Group Info
## 7. `identity.*` — User and Group Info

### Feature: identity.groups
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

### Test Spec: identity.groups
### `identity.groups`

**Happy Path:**

- ✅ Returns Administrators, Users, Guests groups
- ✅ Each group has `members` array
- ✅ Administrators group contains Administrator user
- ✅ `member_count` matches `members.length`

---

### Feature: identity.user.create
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

### Test Spec: identity.user.create
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

### Feature: identity.user.delete
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

### Test Spec: identity.user.delete
### `identity.user.delete`

**Security:**

- 🔒 Requires Dangerous tier
- 🔒 Cannot delete Administrator account
- 🔒 Cannot delete currently logged-on user
- 🔒 Cannot delete account running mcpw

---

### Feature: identity.users
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

### Test Spec: identity.users
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

### Feature: identity.whoami
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

### Test Spec: identity.whoami
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

