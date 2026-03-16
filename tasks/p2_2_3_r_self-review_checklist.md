# Task: `[R]` Self-review checklist

**Phase 2: Windows-Specific Read-Only Domains**
**Sub-phase: 2.3 `printer.*` (16 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[R]` Self-review checklist

## Tool Specifications

### Feature: printer.*
## 19. `printer.*` — Print Management

### Test Spec: printer.*
## 19. `printer.*`

### Feature: printer.* — Print Management
## 19. `printer.*` — Print Management

### Feature: printer.drivers
### `printer.drivers` 🟢 Read

List installed printer drivers.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `drivers` | array | Driver objects |
| `count` | integer | Total count |

**Driver object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Driver name |
| `version` | string | Version |
| `manufacturer` | string / null | Manufacturer |
| `architecture` | string | "x64" / "x86" |
| `monitor_name` | string / null | Port monitor |
| `used_by` | array | Printers using this driver |

**Implementation:** WMI `Win32_PrinterDriver`

---

### Test Spec: printer.drivers
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

### Feature: printer.info
### `printer.info` 🟢 Read

Detailed printer information.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:** All fields from printer object plus:
| Field | Type | Description |
|---|---|---|
| `server_name` | string / null | Print server (null for local) |
| `driver_version` | string | Driver version |
| `print_processor` | string | Print processor name |
| `data_type` | string | Default data type (RAW, EMF) |
| `priority` | integer | Printer priority (1-99) |
| `published` | boolean | Published in AD |
| `paper_sizes` | array | Supported paper sizes |
| `resolutions` | array | Supported DPI values |
| `queued_bytes` | integer | Total bytes in queue |
| `total_pages_printed` | integer | Lifetime pages printed |
| `total_jobs_printed` | integer | Lifetime jobs printed |
| `average_pages_per_minute` | float / null | Rated speed |
| `last_error` | string / null | Last error description |
| `last_error_time` | string / null | ISO 8601 |

**Implementation:** WMI `Win32_Printer` + `Win32_PrinterConfiguration`

---

### Test Spec: printer.info
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

### Feature: printer.job.cancel
### `printer.job.cancel` 🟡 Operate

Cancel a print job.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |
| `document` | string | Document name |
| `cancelled` | boolean | Success |

**Implementation:** WMI `Win32_PrintJob.Delete()`

---

### Test Spec: printer.job.cancel
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

### Feature: printer.job.pause
### `printer.job.pause` 🟡 Operate

Pause a print job.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |
| `paused` | boolean | Success |

**Implementation:** WMI `Win32_PrintJob.Pause()`

---

### Test Spec: printer.job.pause
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

### Feature: printer.job.resume
### `printer.job.resume` 🟡 Operate

Resume a paused print job.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `job_id` | integer | Job ID |
| `resumed` | boolean | Success |

**Implementation:** WMI `Win32_PrintJob.Resume()`

---

### Feature: printer.jobs
### `printer.jobs` 🟢 Read

List print jobs in a printer's queue.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string (optional) | Printer name. If omitted, all printers. |
| `status` | string (optional) | "pending" / "printing" / "error" / "paused" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `jobs` | array | Job objects |
| `total_count` | integer | Total jobs |

**Job object:**
| Field | Type | Description |
|---|---|---|
| `job_id` | integer | Job ID |
| `printer` | string | Printer name |
| `document` | string | Document name |
| `owner` | string | User who submitted |
| `status` | string | "spooling" / "printing" / "paused" / "error" / "deleting" / "offline" / "retained" / "complete" |
| `status_code` | integer | Raw status flags |
| `priority` | integer | Job priority (1-99) |
| `position` | integer | Position in queue |
| `submitted` | string | ISO 8601 submission time |
| `start_time` | string / null | ISO 8601 print start |
| `pages_printed` | integer | Pages printed so far |
| `total_pages` | integer | Total pages |
| `size_bytes` | integer | Job size |
| `data_type` | string | "RAW" / "EMF" |
| `color` | boolean | Color job |

**Implementation:** WMI `Win32_PrintJob`

---

### Test Spec: printer.jobs
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

### Feature: printer.list
### `printer.list` 🟢 Read

List installed printers.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `include_network` | boolean (optional) | Include network printers. Default: true |
| `include_local` | boolean (optional) | Include local printers. Default: true |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printers` | array | Printer objects |
| `count` | integer | Total count |
| `default_printer` | string / null | Name of default printer |

**Printer object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `share_name` | string / null | Share name if shared |
| `port_name` | string | Port (e.g., "USB001", "192.168.1.100", "LPT1") |
| `driver_name` | string | Driver name |
| `location` | string / null | Printer location |
| `comment` | string / null | Description/comment |
| `status` | string | "idle" / "printing" / "error" / "offline" / "paper_jam" / "paper_out" / "toner_low" / "warming_up" / "paused" |
| `is_default` | boolean | Default printer |
| `is_shared` | boolean | Shared on network |
| `is_network` | boolean | Network printer |
| `type` | string | "local" / "network" / "virtual" |
| `color` | boolean / null | Color capable |
| `duplex` | boolean / null | Duplex capable |
| `jobs_count` | integer | Current jobs in queue |

**Implementation:** WMI `Win32_Printer`

---

### Test Spec: printer.list
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

### Feature: printer.pause
### `printer.pause` 🟡 Operate

Pause a printer (holds all new jobs).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `paused` | boolean | Success |
| `pending_jobs` | integer | Jobs currently in queue |

**Implementation:** WMI `Win32_Printer.Pause()`

---

### Test Spec: printer.pause
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

### Feature: printer.ports
### `printer.ports` 🟢 Read

List printer ports.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `ports` | array | Port objects |

**Port object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Port name |
| `type` | string | "tcp" / "usb" / "lpt" / "local" / "wsd" |
| `address` | string / null | IP address (for TCP ports) |
| `queue` | string / null | Remote queue name |
| `protocol` | string / null | "raw" / "lpr" (for TCP ports) |
| `port_number` | integer / null | TCP port (default 9100 for RAW, 515 for LPR) |
| `used_by` | array | Printers using this port |

**Implementation:** WMI `Win32_TCPIPPrinterPort` + `Win32_Port`

---

### Test Spec: printer.ports
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

### Feature: printer.queue.clear
### `printer.queue.clear` 🟡 Operate

Cancel all jobs in a printer's queue.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `printer` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `printer` | string | Printer name |
| `jobs_cancelled` | integer | Number of jobs cancelled |

**Implementation:** WMI enumerate `Win32_PrintJob` where `Name LIKE '{printer}%'` → `Delete()` each

---

### Test Spec: printer.queue.clear
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

### Feature: printer.resume
### `printer.resume` 🟡 Operate

Resume a paused printer.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `resumed` | boolean | Success |
| `pending_jobs` | integer | Jobs that will now process |

**Implementation:** WMI `Win32_Printer.Resume()`

---

### Feature: printer.set_default
### `printer.set_default` 🟡 Operate

Set default printer.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | New default printer |
| `previous_default` | string / null | Previous default |

**Implementation:** WMI `Win32_Printer.SetDefaultPrinter()`

---

### Test Spec: printer.set_default
### `printer.set_default`

**Happy Path:**

- 🎭 Changes default printer → `previous_default` returned
- 🎭 `printer.list` now shows new default with `is_default: true`

**Error Handling:**

- ✅ Non-existent printer → error
- ✅ Already default → succeeds, `previous_default` == `name`

---

### Feature: printer.spooler.clear
### `printer.spooler.clear` 🟡 Operate

Clear the spool directory without restarting (nuclear option for stuck spooler).

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `files_deleted` | integer | Spool files removed |
| `bytes_freed` | integer | Bytes freed |
| `spooler_restarted` | boolean | Whether spooler was restarted after clearing |

**Implementation:** Stop Spooler → delete `*.SHD` and `*.SPL` from spool directory → Start Spooler

---

### Test Spec: printer.spooler.clear
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

### Feature: printer.spooler.restart
### `printer.spooler.restart` 🟡 Operate

Restart the Print Spooler service (common fix for stuck jobs).

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `clear_queue` | boolean (optional) | Delete all spool files before restarting. Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `restarted` | boolean | Success |
| `spool_files_cleared` | integer | Spool files deleted (if clear_queue=true) |
| `elapsed_ms` | integer | Restart time |

**Implementation:** Stop Spooler → optionally delete files in spool directory → Start Spooler

---

### Test Spec: printer.spooler.restart
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

### Feature: printer.spooler.status
### `printer.spooler.status` 🟢 Read

Check Print Spooler service status.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `status` | string | "running" / "stopped" / "paused" |
| `pid` | integer / null | Process ID |
| `startup_type` | string | Service startup type |
| `spool_directory` | string | Spool directory path |
| `spool_size_mb` | float | Current spool directory size |
| `temp_files_count` | integer | Number of spool temp files |

**Implementation:** `ServiceController("Spooler")` + directory enumeration of `%SystemRoot%\System32\spool\PRINTERS`

---

### Test Spec: printer.spooler.status
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

### Feature: printer.test
### `printer.test` 🟡 Operate

Print a test page.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `name` | string | Printer name |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Printer name |
| `test_page_sent` | boolean | Success |
| `job_id` | integer | Test page job ID |

**Implementation:** WMI `Win32_Printer.PrintTestPage()`

---

### Test Spec: printer.test
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

