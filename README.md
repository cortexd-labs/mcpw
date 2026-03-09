# mcpw

**Windows MCP Server** — exposes Windows system administration tools as an [MCP](https://modelcontextprotocol.io/) (Model Context Protocol) server over `stdio`.

Designed to be invoked by [neurond](https://github.com/cortexd-labs/neurond) (federation proxy). Never exposed directly to the network.

---

## Requirements

- Windows 10/11 or Windows Server 2019+
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (build) or .NET 10 Runtime (run pre-built)
- For `iis.*` tools: IIS with Management Console installed
- For `ad.*` / `gpo.*` tools: RSAT or domain-joined machine
- For `hyperv.*` tools: Hyper-V role enabled
- For `container.*` tools: Docker Desktop or Docker Engine (Windows containers)

---

## Build & Run

```powershell
# Run from source
dotnet run --project src/Mcpw

# Publish self-contained single file
dotnet publish src/Mcpw/Mcpw.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/

# Install as Windows Service
.\deploy\install.ps1
```

---

## Testing

```powershell
# All unit tests (no Windows dependencies — everything behind interfaces + mocks)
dotnet test

# Run with filter
dotnet test --filter "Category=Integration"

# Test with MCP Inspector
npx -y @modelcontextprotocol/inspector
# Transport: stdio | Command: .\publish\mcpw.exe
```

---

## Configuration

mcpw reads `C:\ProgramData\mcpw\config.json` at startup. Missing file = all defaults.

```json
{
  "allowedPaths": ["C:\\Users", "C:\\inetpub", "C:\\ProgramData"],
  "blockedPaths": ["C:\\Windows\\System32\\config", "C:\\Windows\\NTDS"],
  "enabledDomains": ["system","process","service","log","network","file","storage","security","container","hardware","schedule","registry","iis"],
  "disabledDomains": ["ad","hyperv","gpo"],
  "privilegeTier": "operate"
}
```

Domains not in `enabledDomains` return `ToolNotFound`. Deploy only what the machine needs.

---

## Architecture

```
mcpw (stdio)
├── Server.cs          JSON-RPC 2.0 router, TextReader/TextWriter based
├── Daemon.cs          IHostedService + Windows Service host (SCM)
├── Config.cs          config.json loader with defaults
├── InputValidator.cs  injection/traversal/blocklist guard
├── IToolHandler.cs    interface for all tool domain classes
│
├── Types/             POCOs serialized in MCP responses
├── Windows/           OS abstractions (interface + implementation)
│   ├── IWmiClient         → WmiClient (System.Management)
│   ├── IEventLogAccess    → EventLogAccess (Eventing.Reader)
│   ├── IRegistryAccess    → RegistryAccess (Microsoft.Win32)
│   ├── IServiceControl    → ServiceControl (ServiceProcess)
│   └── IPowerShellHost    → PowerShellHost (Management.Automation)
│
└── Tools/             One class per domain, all testable via mocked interfaces
    ├── SystemTools    system.*
    ├── ProcessTools   process.*
    ├── ServiceTools   service.*
    ├── LogTools       log.*
    ├── NetworkTools   network.*
    ├── FileTools      file.*
    ├── IdentityTools  identity.*
    ├── StorageTools   storage.*
    ├── SecurityTools  security.*
    ├── ContainerTools container.*
    ├── HardwareTools  hardware.*
    ├── ScheduleTools  schedule.*
    ├── RegistryTools  registry.*   (Windows-only)
    ├── IISTools       iis.*        (Windows-only)
    ├── ADTools        ad.*         (Windows-only)
    ├── HyperVTools    hyperv.*     (Windows-only)
    └── GPOTools       gpo.*        (Windows-only)
```

---

## Privilege Tiers

| Tier | Tools | Required |
|---|---|---|
| **Read** | `system.info`, `process.list`, `service.list`, `log.tail`, `network.*`, `storage.usage`, `registry.get`, `hardware.*` | Any user |
| **Operate** | `service.start/stop`, `process.kill`, `iis.pool.recycle`, `file.write`, `schedule.add` | Local Administrator |
| **Domain** | `ad.*`, `gpo.*`, `hyperv.*` | Domain account |
| **Dangerous** | `system.reboot`, `registry.set/delete`, `ad.user.resetpw` | Explicit neurond policy approval |

---

## Security

- **No network exposure** — stdio only, neurond handles mTLS + auth
- **Input validation** — injection characters rejected, path traversal blocked
- **Path blocklist** — `C:\Windows\System32\config`, `C:\Windows\NTDS` always blocked
- **WQL parameterization** — no string interpolation in WMI queries
- **PowerShell Constrained Language Mode** — all embedded PS runs constrained
- **Registry blocklist** — SAM and SECURITY hives are blocked

---

## Tool Reference

See [tools.md](tools.md) for the full tool reference with input schemas.

---

## Related

- **[mcpd](https://github.com/cortexd-labs/mcpd)** — Linux MCP Server (Rust)
- **[neurond](https://github.com/cortexd-labs/neurond)** — Federation proxy

---

## License

MIT
