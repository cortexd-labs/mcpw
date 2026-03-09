#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Install mcpw as a Windows Service.

.DESCRIPTION
    Publishes a self-contained single-file binary and registers it with the
    Service Control Manager. Requires .NET 10 SDK on the build machine.

.PARAMETER InstallPath
    Directory where mcpw.exe will be placed. Defaults to C:\Program Files\mcpw.

.PARAMETER ConfigPath
    Directory for config.json. Defaults to C:\ProgramData\mcpw.

.PARAMETER ServiceAccount
    Service account to run under. Defaults to LocalService.

.EXAMPLE
    .\install.ps1
    .\install.ps1 -InstallPath "D:\Services\mcpw"
#>
param(
    [string]$InstallPath   = "C:\Program Files\mcpw",
    [string]$ConfigPath    = "C:\ProgramData\mcpw",
    [string]$ServiceAccount = "NT AUTHORITY\LocalService"
)

$ErrorActionPreference = "Stop"
$ServiceName = "mcpw"
$ServiceDisplay = "mcpw — Windows MCP Server"
$ServiceDescription = "Exposes Windows system administration tools as an MCP server over stdio. Managed by neurond."

# ── 1. Build ─────────────────────────────────────────────────────────────────

Write-Host "Building mcpw (self-contained, win-x64)..."
$projectRoot = Join-Path $PSScriptRoot ".."
Push-Location $projectRoot

dotnet publish src/Mcpw/Mcpw.csproj `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -o "$env:TEMP\mcpw-publish"

if ($LASTEXITCODE -ne 0) {
    Pop-Location
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}
Pop-Location

# ── 2. Install binary ─────────────────────────────────────────────────────────

Write-Host "Installing to $InstallPath..."
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
Copy-Item "$env:TEMP\mcpw-publish\mcpw.exe" -Destination $InstallPath -Force

# ── 3. Create config directory + default config ───────────────────────────────

New-Item -ItemType Directory -Force -Path $ConfigPath | Out-Null
$defaultConfig = "$ConfigPath\config.json"
if (-not (Test-Path $defaultConfig)) {
    @{
        allowedPaths    = @("C:\Users", "C:\inetpub", "C:\ProgramData")
        blockedPaths    = @("C:\Windows\System32\config", "C:\Windows\NTDS")
        enabledDomains  = @("system","process","service","log","network","file","storage","security","container","hardware","schedule","registry","iis")
        disabledDomains = @("ad","hyperv","gpo")
        privilegeTier   = "operate"
    } | ConvertTo-Json -Depth 3 | Set-Content $defaultConfig
    Write-Host "Default config written to $defaultConfig"
}

# ── 4. Register service ───────────────────────────────────────────────────────

$exePath = Join-Path $InstallPath "mcpw.exe"

# Remove existing service if present
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Removing existing $ServiceName service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep 2
}

Write-Host "Creating Windows service '$ServiceName'..."
sc.exe create $ServiceName `
    binPath= "`"$exePath`"" `
    start= auto `
    obj= $ServiceAccount | Out-Null

sc.exe description $ServiceName $ServiceDescription | Out-Null
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null

# ── 5. Start ──────────────────────────────────────────────────────────────────

Write-Host "Starting $ServiceName..."
Start-Service -Name $ServiceName
$svc = Get-Service -Name $ServiceName
Write-Host "Service status: $($svc.Status)"

Write-Host ""
Write-Host "mcpw installed successfully."
Write-Host "  Binary : $exePath"
Write-Host "  Config : $defaultConfig"
Write-Host "  Service: $ServiceName ($($svc.Status))"
