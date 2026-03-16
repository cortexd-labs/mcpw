# Task: HardwareTools.cs

**Phase 1: Shared Read-Only Domains (Lowest Risk)**
**Sub-phase: 1.12 `hardware.*` (8 tools)**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **HardwareTools.cs** — Implement using `IWmiClient` (`Win32_PnPEntity`, `Win32_BIOS`, `Win32_PhysicalMemory`, `Win32_VideoController`, `Win32_Battery`, `MSAcpi_ThermalZoneTemperature`, `Win32_Processor`, `Win32_Fan`, `Win32_TemperatureProbe`, `Win32_VoltageProbe`)
  - File: `src/Mcpw/Tools/HardwareTools.cs`
  - **New types needed in `HardwareTypes.cs`**: `SensorData` (thermal_zones, cpu_temperatures, fan_speeds, voltages), `CpuDetailInfo` (name, max/current clock, load, cores, throttled, voltage, cache sizes, socket)

## Tool Specifications

### Feature: hardware.*
## 12. `hardware.*` — Hardware Enumeration

### Test Spec: hardware.*
## 12. `hardware.*`

### Feature: hardware.* — Hardware Enumeration
## 12. `hardware.*` — Hardware Enumeration

### Feature: hardware.battery
### `hardware.battery` 🟢 Read

Battery status (laptops/UPS).

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `batteries` | array | Battery objects |
| `on_ac_power` | boolean | Whether on AC power |

**Battery object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Battery name |
| `status` | string | "charging" / "discharging" / "full" / "not_charging" |
| `charge_percent` | integer | Charge percentage |
| `estimated_runtime_minutes` | integer / null | Estimated minutes remaining |
| `design_capacity_mwh` | integer | Design capacity |
| `full_charge_capacity_mwh` | integer | Current full charge capacity |
| `health_percent` | float | Battery health (full charge / design capacity) |
| `cycle_count` | integer / null | Charge cycle count |

**Implementation:** WMI `Win32_Battery` + `BatteryStatus`

---

### Test Spec: hardware.battery
### `hardware.battery`

**Happy Path:**

- ✅ Laptop → returns battery info with `charge_percent`
- ✅ `on_ac_power` correctly reflects power state
- ✅ `health_percent` calculated correctly

**Error Handling:**

- ✅ Desktop / Server (no battery) → empty `batteries` array, `on_ac_power: true`
- ✅ UPS battery → may appear depending on driver

**Edge Cases:**

- ⚡ Battery fully charged → `status: "full"`, `charge_percent: 100`
- ⚡ Battery critically low → `charge_percent` < 5
- ⚡ No battery firmware info → null fields

---

### Feature: hardware.bios
### `hardware.bios` 🟢 Read

BIOS/UEFI and motherboard information.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `bios_vendor` | string | BIOS manufacturer |
| `bios_version` | string | BIOS version string |
| `bios_date` | string | BIOS release date |
| `bios_mode` | string | "UEFI" / "Legacy" |
| `secure_boot` | boolean | Secure Boot enabled |
| `motherboard_manufacturer` | string | Board manufacturer |
| `motherboard_product` | string | Board product name |
| `motherboard_serial` | string | Board serial |
| `system_manufacturer` | string | System manufacturer |
| `system_model` | string | System model |
| `system_serial` | string | System serial number |
| `uuid` | string | System UUID |

**Implementation:** WMI `Win32_BIOS` + `Win32_BaseBoard` + `Win32_ComputerSystemProduct`

---

### Test Spec: hardware.bios
### `hardware.bios`

**Happy Path:**

- ✅ Returns `bios_vendor` non-empty
- ✅ Returns `bios_version` non-empty
- ✅ Returns `bios_mode` as "UEFI" or "Legacy"
- ✅ Returns `secure_boot` boolean
- ✅ Returns `motherboard_manufacturer` and `motherboard_product`
- ✅ Returns `system_serial` (may be "To Be Filled" in VMs)
- ✅ Returns valid `uuid`

**Edge Cases:**

- ⚡ Virtual machine → vendor is "Hyper-V", "VMware", "QEMU"
- ⚡ Fields filled with "To Be Filled By O.E.M." → returned as-is
- ⚡ `secure_boot` on Legacy BIOS → `false`

---

### Feature: hardware.gpu
### `hardware.gpu` 🟢 Read

GPU/display adapter information.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `adapters` | array | GPU objects |

**GPU object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Adapter name |
| `manufacturer` | string | Manufacturer |
| `driver_version` | string | Driver version |
| `driver_date` | string | Driver date |
| `vram_bytes` | integer | Video RAM |
| `resolution` | string | Current resolution (e.g., "1920x1080") |
| `refresh_rate_hz` | integer | Current refresh rate |
| `status` | string | "ok" / "error" |

**Implementation:** WMI `Win32_VideoController`

---

### Test Spec: hardware.gpu
### `hardware.gpu`

**Happy Path:**

- ✅ Returns at least one adapter (even basic display)
- ✅ `name` is non-empty
- ✅ `vram_bytes` > 0 for discrete GPU
- ✅ `resolution` in format "WIDTHxHEIGHT"
- ✅ `driver_version` is non-empty

**Edge Cases:**

- ⚡ Server Core (no GUI) → basic display adapter or none
- ⚡ Remote Desktop session → RDP display adapter
- ⚡ Multiple GPUs → all listed
- ⚡ Hyper-V VM → "Microsoft Hyper-V Video" adapter

---

### Feature: hardware.memory
### `hardware.memory` 🟢 Read

Physical memory module details.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `modules` | array | Memory module objects |
| `total_slots` | integer | Total DIMM slots |
| `used_slots` | integer | Occupied slots |
| `total_capacity_gb` | float | Total installed RAM |

**Module object:**
| Field | Type | Description |
|---|---|---|
| `slot` | string | Physical slot (e.g., "DIMM_A1") |
| `capacity_gb` | float | Module capacity |
| `speed_mhz` | integer | Speed in MHz |
| `type` | string | "DDR4" / "DDR5" |
| `manufacturer` | string | Module manufacturer |
| `part_number` | string | Part number |
| `serial_number` | string | Serial number |

**Implementation:** WMI `Win32_PhysicalMemory` + `Win32_PhysicalMemoryArray`

---

### Test Spec: hardware.memory
### `hardware.memory`

**Happy Path:**

- ✅ Returns at least one memory module
- ✅ `total_capacity_gb` > 0
- ✅ `used_slots` <= `total_slots`
- ✅ Sum of module capacities ≈ `total_capacity_gb`
- ✅ Each module has `speed_mhz`, `type`

**Edge Cases:**

- ⚡ Virtual machine → single module reported, may lack manufacturer info
- ⚡ System with empty DIMM slots → shown in `total_slots` but not in `modules`
- ⚡ Mixed memory speeds → each module shows its own speed

---

### Feature: hardware.pci
### `hardware.pci` 🟢 Read

List PCI devices.

**Input:**
| Parameter | Type | Description |
|---|---|---|
| `class` | string (optional) | Filter by device class: "display" / "network" / "storage" / "audio" |

**Response:**
| Field | Type | Description |
|---|---|---|
| `devices` | array | PCI device objects |
| `count` | integer | Total count |

**Device object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Device name |
| `description` | string | Description |
| `manufacturer` | string | Manufacturer |
| `device_id` | string | Hardware ID (VEN_XXXX&DEV_XXXX) |
| `class` | string | Device class |
| `driver_version` | string / null | Installed driver version |
| `driver_date` | string / null | Driver date |
| `status` | string | "ok" / "error" / "disabled" |
| `location` | string | Bus/device/function location |

**Implementation:** WMI `Win32_PnPEntity` with `PNPClass` filter

---

### Test Spec: hardware.pci
### `hardware.pci`

**Happy Path:**

- ✅ Returns non-empty list (system always has PCI devices)
- ✅ Contains display adapter
- ✅ Contains storage controller
- ✅ `class: "display"` filters to display adapters only
- ✅ `class: "network"` filters to network adapters
- ✅ Each device has `name`, `device_id`, `status`
- ✅ `driver_version` populated for devices with drivers

**Edge Cases:**

- ⚡ Device with no driver → `driver_version: null`
- ⚡ Disabled device → `status: "disabled"`
- ⚡ Virtual machine with few PCI devices
- ⚡ `class` filter with no matches → empty array

---

### Feature: hardware.usb
### `hardware.usb` 🟢 Read

List USB devices.

**Input:** None

**Response:**
| Field | Type | Description |
|---|---|---|
| `devices` | array | USB device objects |
| `count` | integer | Total count |

**USB device object:**
| Field | Type | Description |
|---|---|---|
| `name` | string | Device name |
| `device_id` | string | USB VID/PID |
| `manufacturer` | string / null | Manufacturer |
| `description` | string | Description |
| `status` | string | "ok" / "error" |
| `class` | string | Device class (e.g., "HID", "Storage", "Printer") |
| `hub_port` | string / null | USB hub and port info |

**Implementation:** WMI `Win32_USBControllerDevice` + `Win32_PnPEntity`

---

### Test Spec: hardware.usb
### `hardware.usb`

**Happy Path:**

- ✅ Returns list of USB devices (may be empty in VM)
- ✅ Each device has `name`, `device_id`
- ✅ `class` identifies device type

**Edge Cases:**

- ⚡ No USB devices (virtual machine) → empty array
- ⚡ USB hub (parent device) appears in list
- ⚡ USB device without manufacturer info → `manufacturer: null`

---

