# Task: `[T]` Create `tests/Tools/HyperVToolsTests.cs`

**Phase 4: Domain-Tier Tools (AD, Hyper-V, GPO)**
**Sub-phase: 4.2 `hyperv.*` (6 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[T]` Create `tests/Tools/HyperVToolsTests.cs`
  - `hyperv.vms` (6 tests): returns VMs, state running/off filter, required fields, running VM memory > 0, Hyper-V not installed error, not enabled error, no VMs empty
  - `hyperv.vm.info` (10 tests): full details by name, by GUID, disks array, network_adapters, running cpu_usage, guest_os, not found error, no disks, no network, Gen 1 vs Gen 2, multiple disks, ISO mounted, snapshots, dynamic memory
  - `hyperv.vm.start` (5 tests): starts VM → running, already running error, saved state resumes, missing VHD error, insufficient resources
  - `hyperv.vm.stop` (5 tests): force false graceful, force true turn off, already off error, no IC → timeout suggest force, paused can stop
  - `hyperv.vm.restart` (3 tests): force false graceful, force true reset, VM off error
  - `hyperv.vm.snapshot` (5 tests): creates checkpoint, custom name, auto-generated name with timestamp, appears in info, insufficient disk, not found, running vs off
  - `hyperv.switches` (3 tests): lists switches, type/interface, connected_vms, no switches, default switch, external NIC team

## Tool Specifications

### Feature: hyperv.*
## 17. `hyperv.*` — Hyper-V Virtual Machines

### Test Spec: hyperv.*
## 17. `hyperv.*`

### Feature: hyperv.* — Hyper-V Virtual Machines
## 17. `hyperv.*` — Hyper-V Virtual Machines

### Feature: hyperv.switches
### `hyperv.switches` 🔵 Domain

List virtual switches.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `switches` | array | Switch objects |

**Switch object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Switch name |
| `id` | string | Switch GUID |
| `type` | string | "external" / "internal" / "private" |
| `interface` | string / null | Bound physical NIC (external only) |
| `management_os` | boolean | Allow host OS access |
| `connected_vms` | integer | VMs connected |

**Implementation:** WMI `Msvm_VirtualEthernetSwitch`

---

### Test Spec: hyperv.switches
### `hyperv.switches`

**Happy Path:**

- 🧪 Lists virtual switches
- 🧪 Each switch has `type`, `interface` (for external)
- 🧪 `connected_vms` count is accurate

**Edge Cases:**

- ⚡ No switches configured → empty array
- ⚡ Default switch (auto-created) → listed
- ⚡ External switch bound to team NIC

---

### Feature: hyperv.vm.info
### `hyperv.vm.info` 🔵 Domain

Detailed VM information.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |

**Response:** All fields from VM object plus:
| Field | Type | Description |
|---|---|---|
| `disks` | array | `[{controller, path, size_gb, format, type}]` — VHD/VHDX details |
| `network_adapters` | array | `[{name, switch_name, mac_address, vlan_id, ip_addresses}]` |
| `dvd_drives` | array | `[{controller, path}]` |
| `snapshots` | array | `[{name, created, parent}]` — checkpoint tree |
| `replication_state` | string / null | Replication status if configured |
| `memory_demand_mb` | integer / null | Current memory demand (if running) |
| `cpu_usage_percent` | float / null | Current CPU usage (if running) |
| `heartbeat` | string / null | Guest heartbeat status |
| `guest_os` | string / null | Detected guest OS |
| `secure_boot` | boolean | Secure Boot enabled (Gen 2) |
| `tpm` | boolean | vTPM enabled |
| `automatic_start_action` | string | "nothing" / "start_if_running" / "always_start" |
| `automatic_stop_action` | string | "save" / "turn_off" / "shutdown" |

**Implementation:** WMI `Msvm_*` associated objects (storage, network, memory, processor settings)

---

### Test Spec: hyperv.vm.info
### `hyperv.vm.info`

**Happy Path:**

- 🧪 Returns full details for valid VM name
- 🧪 Returns full details for valid VM GUID
- 🧪 `disks` array lists VHD/VHDX with paths and sizes
- 🧪 `network_adapters` lists adapters with switch assignments
- 🧪 Running VM → `cpu_usage_percent` populated
- 🧪 Running VM → `guest_os` detected via integration services

**Error Handling:**

- ✅ VM not found → error
- ✅ VM with no disks → empty `disks` array
- ✅ VM with no network → empty `network_adapters`

**Edge Cases:**

- ⚡ Generation 1 vs Generation 2 → different capabilities shown
- ⚡ VM with multiple disks on different controllers
- ⚡ VM with ISO mounted → appears in `dvd_drives`
- ⚡ VM with snapshots → `snapshots` array populated
- ⚡ VM with dynamic memory → `memory_assigned_mb` may differ from `memory_startup_mb`

---

### Feature: hyperv.vm.restart
### `hyperv.vm.restart` 🔵 Domain

Restart a VM.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |
| `force` | boolean (optional) | Hard reset (vs. graceful restart). Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | VM name |
| `method` | string | "restart" / "reset" |
| `state` | string | Current state |

**Implementation:** Graceful: shutdown + start. Force: `RequestStateChange(10)` — 10 = Reset

---

### Test Spec: hyperv.vm.restart
### `hyperv.vm.restart`

**Happy Path:**

- 🧪 `force: false` → graceful restart
- 🧪 `force: true` → hard reset

**Error Handling:**

- ✅ VM is off → error (can't restart what's not running)

---

### Feature: hyperv.vm.snapshot
### `hyperv.vm.snapshot` 🔵 Domain

Create a checkpoint/snapshot.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |
| `name` | string (optional) | Checkpoint name. Default: auto-generated with timestamp |

**Response:**
| Field | Type | Description |
|---|---|---|
| `vm_name` | string | VM name |
| `snapshot_name` | string | Checkpoint name |
| `created` | boolean | Success |
| `snapshot_id` | string | Snapshot GUID |

**Implementation:** `Msvm_VirtualSystemSnapshotService.CreateSnapshot()`

---

### Test Spec: hyperv.vm.snapshot
### `hyperv.vm.snapshot`

**Happy Path:**

- 🧪 Creates checkpoint → `snapshot_id` returned
- 🧪 Custom name → `snapshot_name` matches
- 🧪 Auto-generated name includes timestamp
- 🧪 Snapshot appears in `hyperv.vm.info` snapshots list

**Error Handling:**

- ✅ Insufficient disk space → error
- ✅ VM not found → error

**Edge Cases:**

- ⚡ Snapshot while VM is running (online checkpoint)
- ⚡ Snapshot of off VM (offline checkpoint)
- ⚡ Multiple snapshots → tree maintained correctly

---

### Feature: hyperv.vm.start
### `hyperv.vm.start` 🔵 Domain

Start a VM.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | VM name |
| `state` | string | New state |
| `started` | boolean | Success |

**Implementation:** `Msvm_ComputerSystem.RequestStateChange(2)` — 2 = Enabled/Running

---

### Test Spec: hyperv.vm.start
### `hyperv.vm.start`

**Happy Path:**

- 🧪 Start off VM → `state: "running"`, `started: true`

**Error Handling:**

- ✅ Already running → error
- ✅ VM in saved state → start resumes from saved state
- ✅ VM with missing VHD → error "Virtual hard disk not found"
- ✅ VM config corrupted → error
- ✅ Insufficient resources (memory) → error

**Security:**

- 🔒 Requires Domain tier

---

### Feature: hyperv.vm.stop
### `hyperv.vm.stop` 🔵 Domain

Stop a VM.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | VM name or GUID |
| `force` | boolean (optional) | Turn off (vs. graceful shutdown). Default: false |

**Response:**
| Field | Type | Description |
|---|---|---|
| `name` | string | VM name |
| `state` | string | New state |
| `method` | string | "shutdown" / "turn_off" |
| `stopped` | boolean | Success |

**Implementation:** Graceful: `Msvm_ShutdownComponent.InitiateShutdown()`. Force: `RequestStateChange(3)` — 3 = Disabled/Off

---

### Test Spec: hyperv.vm.stop
### `hyperv.vm.stop`

**Happy Path:**

- 🧪 `force: false` → graceful shutdown via integration services
- 🧪 `force: true` → immediate turn off

**Error Handling:**

- ✅ Already off → error
- ✅ Guest doesn't respond to shutdown (no IC) → timeout, suggest force
- ✅ VM in paused state → can be stopped

**Edge Cases:**

- ⚡ VM without integration services → graceful shutdown fails, need force
- ⚡ VM running critical workload → warning before force

---

### Feature: hyperv.vms
### `hyperv.vms` 🔵 Domain

List Hyper-V virtual machines.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `state` | string (optional) | "running" / "off" / "saved" / "paused" / "all". Default: "all" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `vms` | array | VM objects |
| `count` | integer | Total count |

**VM object:**
| Field | Type | Description |
|---|---|---|
| `id` | string | VM GUID |
| `name` | string | VM name |
| `state` | string | "running" / "off" / "saved" / "paused" / "starting" / "saving" / "stopping" / "reset" |
| `generation` | integer | 1 or 2 |
| `version` | string | Configuration version |
| `cpu_count` | integer | Virtual processor count |
| `memory_assigned_mb` | integer | Currently assigned RAM |
| `memory_startup_mb` | integer | Startup RAM |
| `memory_dynamic` | boolean | Dynamic memory enabled |
| `uptime` | string | Uptime (human-readable) |
| `status` | string | "Operating normally" / "Critical" / etc. |
| `integration_services_version` | string / null | IC version |
| `checkpoint_count` | integer | Number of checkpoints |
| `path` | string | Configuration path |
| `notes` | string | VM notes |

**Implementation:** WMI `Msvm_ComputerSystem` (root/virtualization/v2)

---

### Test Spec: hyperv.vms
### `hyperv.vms`

**Happy Path:**

- 🧪 Returns list of VMs
- 🧪 `state: "running"` only returns running VMs
- 🧪 `state: "off"` only returns off VMs
- 🧪 Each VM has `id`, `name`, `state`, `generation`
- 🧪 Running VM has `memory_assigned_mb` > 0

**Error Handling:**

- ✅ Hyper-V not installed → error "Hyper-V not available"
- ✅ Hyper-V not enabled → error
- ✅ No VMs → empty array

---

