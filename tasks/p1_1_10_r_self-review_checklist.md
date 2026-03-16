# Task: `[R]` Self-review checklist

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.10 `security.*` (5 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` Self-review checklist

## Tool Specifications

### Test Spec: Security (Global)
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

### Feature: security.*
## 10. `security.*` — Security Checks

### Test Spec: security.*
## 10. `security.*`

### Feature: security.* — Security Checks
## 10. `security.*` — Security Checks

### Feature: security.audit_policy
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

### Test Spec: security.audit_policy
### `security.audit_policy`

**Happy Path:**

- ✅ Returns non-empty categories
- ✅ Contains "Account Logon", "Logon/Logoff", "Object Access"
- ✅ Each subcategory has `success` and `failure` booleans

---

### Feature: security.certs
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

### Test Spec: security.certs
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

### Feature: security.defender
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

### Test Spec: security.defender
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

### Feature: security.local_policy
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

### Test Spec: security.local_policy
### `security.local_policy`

**Happy Path:**

- ✅ Returns `password_policy` with `min_length`, `max_age_days`, etc.
- ✅ Returns `user_rights` array
- ✅ `complexity_required` is boolean

---

### Feature: security.open_ports
### `security.open_ports` 🟢 Read

Alias for `network.ports`. Lists all listening ports with owning process.

---

### Feature: security.windows_update
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

### Test Spec: security.windows_update
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

