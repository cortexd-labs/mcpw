# Task: Complete all Types/

**Phase 0: Infrastructure & Core (Foundation)**
**Sub-phase: 0.7 Domain Type POCOs**

## Global References
- [Conventions](00_Conventions.md)
- [Security Standards](00_Security_Standards.md)
- [MCP Protocol](00_MCP_Protocol.md)
- [Configuration Reference](00_Configuration_Reference.md)

## Task Status
- [ ] `[I]` **Complete all Types/**:
  - `SystemTypes.cs` — `SystemInfo`, `UptimeInfo`, `EnvVar`, `SysctlParam`
  - `ProcessTypes.cs` — `ProcessInfo`, `ProcessTree`, `TopResult`
  - `ServiceTypes.cs` — `ServiceInfo`, `ServiceConfig`
  - `LogTypes.cs` — `LogEntry`, `LogChannel`
  - `NetworkTypes.cs` — `NicInfo`, `PortListener`, `Connection`, `FirewallRule`, `Route`, `PingResult`, `TracerouteHop`
  - `FileTypes.cs` — `FileMetadata`, `SearchResult`, `ShareInfo`
  - `IdentityTypes.cs` — `LocalUser`, `LocalGroup`, `WhoAmI`
  - `StorageTypes.cs` — `DiskInfo`, `VolumeInfo`, `SmartData`, `PartitionInfo`
  - `ScheduleTypes.cs` — `ScheduledTask`, `TaskTrigger`, `TaskAction`
  - `SecurityTypes.cs` — `CertInfo`, `AuditPolicy`, `LocalPolicy`, `UpdateStatus`, `DefenderStatus`
  - `ContainerTypes.cs` — `ContainerInfo`, `ContainerImage`, `ExecResult`
  - `HardwareTypes.cs` — `PciDevice`, `UsbDevice`, `BiosInfo`, `MemoryModule`, `GpuInfo`, `BatteryInfo`, `SensorData`, `ThermalZone`, `CpuTemperature`, `FanSpeed`, `VoltageReading`, `CpuDetailInfo`
  - `RegistryTypes.cs` — `RegistryValue`, `RegistryKeyInfo`, `RegistrySearchResult`
  - `IISTypes.cs` — `IisSite`, `AppPool`, `WorkerProcess`, `SiteConfig`
  - `ADTypes.cs` — `AdUser`, `AdGroup`, `AdComputer`, `OuInfo`
  - `HyperVTypes.cs` — `VmInfo`, `VirtualSwitch`, `SnapshotInfo`
  - Files: `src/Mcpw/Types/*.cs`

## Tool Specifications

