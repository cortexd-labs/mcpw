# Task: `[T]` Create `tests/Tools/StorageToolsTests.cs`

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.8 `storage.*` (5 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` Create `tests/Tools/StorageToolsTests.cs`
  - `storage.disks` (8 tests): at least one disk, size > 0, status OK, partitions > 0, media_type returns HDD/SSD/NVMe correctly, interface returns SATA/NVMe/SAS/USB, firmware_version non-empty, status "Degraded" for failing disk, status "Error" for faulted disk, serial number non-empty

## Tool Specifications

### Feature: storage.*
## 8. `storage.*` — Disk and Storage

### Test Spec: storage.*
## 8. `storage.*`

### Feature: storage.* — Disk and Storage
## 8. `storage.*` — Disk and Storage

### Feature: storage.disks
### `storage.disks` 🟢 Read

List physical disks.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `disks` | array | Disk objects |
| `count` | integer | Total count |

**Disk object:**
| Field | Type | Description |
|---|---|---|
| `index` | integer | Disk number |
| `model` | string | Disk model |
| `serial` | string | Serial number |
| `size_bytes` | integer | Total capacity |
| `size_human` | string | Human-readable size |
| `media_type` | string | "HDD" / "SSD" / "NVMe" / "Unknown" |
| `interface` | string | "SATA" / "NVMe" / "SAS" / "USB" |
| `partitions` | integer | Partition count |
| `status` | string | "OK" / "Degraded" / "Error" |
| `firmware_version` | string | Firmware version |
| `bus_type` | string | Bus type |

**Implementation:** WMI `Win32_DiskDrive` + `MSFT_PhysicalDisk` (Storage Spaces)

---

### Test Spec: storage.disks
### `storage.disks`

**Happy Path:**

- ✅ Returns at least one disk (system disk)
- ✅ `size_bytes` > 0
- ✅ `status: "OK"` for healthy disk
- ✅ `partitions` count > 0 for system disk

---

### Feature: storage.partitions
### `storage.partitions` 🟢 Read

List disk partitions.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `disk` | integer (optional) | Disk index. If omitted, all disks. |

**Response:**
| Field | Type | Description |
|---|---|---|
| `partitions` | array | Partition objects |

**Partition object:**
| Field | Type | Description |
|---|---|---|
| `disk_index` | integer | Parent disk |
| `index` | integer | Partition number |
| `type` | string | "GPT" / "MBR" |
| `gpt_type` | string / null | GPT type GUID name (e.g., "EFI System", "Basic Data") |
| `size_bytes` | integer | Partition size |
| `offset_bytes` | integer | Starting offset |
| `drive_letter` | string / null | Assigned drive letter |
| `is_boot` | boolean | Boot partition |
| `is_active` | boolean | Active partition |

**Implementation:** WMI `Win32_DiskPartition` + `MSFT_Partition`

---

### Test Spec: storage.partitions
### `storage.partitions`

**Happy Path:**

- ✅ System disk has at least 1 partition
- ✅ EFI System partition exists on UEFI systems
- ✅ `type` is "GPT" or "MBR"
- ✅ Partitions' sizes sum to approximately disk size

---

### Feature: storage.smart
### `storage.smart` 🟢 Read

SMART health data for physical disks.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `disk` | integer (optional) | Disk index. If omitted, all disks. |

**Response:**
| Field | Type | Description |
|---|---|---|
| `disks` | array | SMART data per disk |

**SMART object:**
| Field | Type | Description |
|---|---|---|
| `disk_index` | integer | Disk number |
| `model` | string | Disk model |
| `status` | string | "OK" / "Caution" / "Bad" |
| `temperature_celsius` | integer / null | Current temperature |
| `power_on_hours` | integer / null | Total power-on hours |
| `reallocated_sectors` | integer / null | Reallocated sector count |
| `pending_sectors` | integer / null | Pending sector count |
| `attributes` | array | `[{id, name, value, worst, threshold, raw}]` |

**Implementation:** WMI `MSStorageDriver_ATAPISmartData` + `MSStorageDriver_FailurePredictStatus`

---

### Test Spec: storage.smart
### `storage.smart`

**Happy Path:**

- ✅ Returns SMART data for physical disk
- ✅ `status` is "OK" for healthy disk
- ✅ `attributes` array contains SMART attributes

**Error Handling:**

- ✅ SSD without traditional SMART → returns NVMe health info or partial data
- ✅ USB drive (no SMART) → error or empty attributes
- ✅ Virtual disk (Hyper-V, VMware) → no SMART data available, clear error

---

### Feature: storage.usage
### `storage.usage` 🟢 Read

Disk space usage summary.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `path` | string (optional) | Get usage for specific path. If omitted, all drives. |

**Response:**
| Field | Type | Description |
|---|---|---|
| `drives` | array | `[{letter, label, total_gb, used_gb, free_gb, used_percent}]` |
| `total_gb` | float | Total capacity across all drives |
| `used_gb` | float | Total used |
| `free_gb` | float | Total free |

**Implementation:** `DriveInfo.GetDrives()`

---

### Test Spec: storage.usage
### `storage.usage`

**Happy Path:**

- ✅ Returns all drives with space info
- ✅ `total_gb` is sum of all drive totals
- ✅ `path` parameter returns info for specific path's drive

**Edge Cases:**

- ⚡ `path` that doesn't exist → error
- ⚡ Network drive → included if mapped

---

### Feature: storage.volumes
### `storage.volumes` 🟢 Read

List volumes and mount points.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `volumes` | array | Volume objects |
| `count` | integer | Total count |

**Volume object:**
| Field | Type | Description |
|---|---|---|
| `drive_letter` | string / null | Drive letter (e.g., "C:") |
| `label` | string | Volume label |
| `file_system` | string | "NTFS" / "ReFS" / "FAT32" / "exFAT" |
| `size_bytes` | integer | Total capacity |
| `free_bytes` | integer | Free space |
| `used_bytes` | integer | Used space |
| `used_percent` | float | Usage percentage |
| `type` | string | "fixed" / "removable" / "network" / "cdrom" / "ram" |
| `mount_point` | string | Mount point path |
| `serial_number` | string | Volume serial number |
| `compressed` | boolean | Compression enabled |
| `bitlocker_status` | string / null | "encrypted" / "decrypted" / "encrypting" / null |

**Implementation:** `DriveInfo.GetDrives()` + WMI `Win32_Volume` + `Win32_EncryptableVolume` for BitLocker

---

### Test Spec: storage.volumes
### `storage.volumes`

**Happy Path:**

- ✅ Returns C: volume
- ✅ `used_bytes` + `free_bytes` ≈ `size_bytes` (within 1%)
- ✅ `file_system` is "NTFS" or "ReFS" for system volume
- ✅ `used_percent` between 0 and 100

**Edge Cases:**

- ⚡ Volume without drive letter (mounted as folder)
- ⚡ CD-ROM drive with no disc → `type: "cdrom"`, minimal info
- ⚡ BitLocker encrypted volume → `bitlocker_status: "encrypted"`

---

