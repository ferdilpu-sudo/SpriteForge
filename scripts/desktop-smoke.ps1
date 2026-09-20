param(
    [string]$PackageRoot = '',
    [int]$StartupSeconds = 8
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
    $PackageRoot = Join-Path $Root 'artifacts\SpriteForge-v1-rc-win-x64'
} elseif (-not [System.IO.Path]::IsPathRooted($PackageRoot)) {
    $PackageRoot = Join-Path $Root $PackageRoot
}

$ExePath = Join-Path $PackageRoot 'SpriteForge.App.exe'
if (-not (Test-Path $ExePath)) {
    throw "SpriteForge RC executable was not found at $ExePath. Run .\scripts\package-rc.ps1 first."
}

Write-Host "Launching SpriteForge RC..."
$Process = Start-Process -FilePath $ExePath -WorkingDirectory $PackageRoot -PassThru
Start-Sleep -Seconds $StartupSeconds
$Process.Refresh()

if ($Process.HasExited) {
    throw "SpriteForge exited during the startup smoke window with code $($Process.ExitCode)."
}

Write-Host "Startup smoke passed: SpriteForge is still running after $StartupSeconds seconds."
Write-Host "Continue the interactive checks in docs\V1-ACCEPTANCE.md."
