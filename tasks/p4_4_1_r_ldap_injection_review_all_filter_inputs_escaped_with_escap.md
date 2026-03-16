# Task: `[R]` LDAP injection review: all filter inputs escaped with `EscapeLdapFilterValue()`

**Phase 4: Domain-Tier Tools (AD, Hyper-V, GPO)**
**Sub-phase: 4.1 `ad.*` (11 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` LDAP injection review: all filter inputs escaped with `EscapeLdapFilterValue()`

## Tool Specifications

### Feature: ad.*
## 16. `ad.*` — Active Directory

### Test Spec: ad.*
## 16. `ad.*`

### Feature: ad.* — Active Directory
## 16. `ad.*` — Active Directory

### Feature: ad.computers
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

### Test Spec: ad.computers
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

### Feature: ad.group.members
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

### Test Spec: ad.group.members
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

### Feature: ad.groups
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

### Test Spec: ad.groups
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

### Feature: ad.ou.list
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

### Test Spec: ad.ou.list
### `ad.ou.list`

**Happy Path:**

- 🧪 Returns OU tree from domain root
- 🧪 `root` parameter starts from specific OU
- 🧪 `depth: 1` only immediate children
- 🧪 Each OU has child counts (users, computers, groups)
- 🧪 `gpo_links` lists linked GPOs

---

### Feature: ad.user.disable
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

### Feature: ad.user.enable
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

### Test Spec: ad.user.enable
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

### Feature: ad.user.groups
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

### Test Spec: ad.user.groups
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

### Feature: ad.user.info
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

### Test Spec: ad.user.info
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

### Feature: ad.user.resetpw
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

### Test Spec: ad.user.resetpw
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

### Feature: ad.user.unlock
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

### Test Spec: ad.user.unlock
### `ad.user.unlock`

**Happy Path:**

- 🧪 Unlocks locked user → `was_locked: true`, `unlocked: true`

**Error Handling:**

- ✅ User not locked → `was_locked: false`, `unlocked: true`
- ✅ User not found → error

---

### Feature: ad.users
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

### Test Spec: ad.users
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

