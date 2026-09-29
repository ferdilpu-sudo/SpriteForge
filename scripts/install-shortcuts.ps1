<#
.SYNOPSIS
  Creates (or removes) Start Menu and optional Desktop shortcuts for SpriteForge.

.DESCRIPTION
  SpriteForge ships as an unpackaged app, so Windows does not add it to the
  Start Menu automatically. This script creates .lnk shortcuts that use the
  embedded SpriteForge icon. Pinning the Start Menu shortcut to the taskbar
  keeps the icon on the taskbar as well.
#>
param(
    [string]$ExePath = '',
    [switch]$Desktop,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $Candidates = @(
        (Join-Path $PSScriptRoot '..\SpriteForge.App.exe'),
        (Join-Path $PSScriptRoot '..\src\SpriteForge.App\bin\x64\Release\net10.0-windows10.0.19041.0\win-x64\SpriteForge.App.exe')
    )
    $ExePath = $Candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

$StartMenuLink = Join-Path ([Environment]::GetFolderPath('Programs')) 'SpriteForge.lnk'
$DesktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'SpriteForge.lnk'

if ($Remove) {
    foreach ($Link in @($StartMenuLink, $DesktopLink)) {
        if (Test-Path $Link) {
            Remove-Item -Force $Link
            Write-Host "Removed $Link"
        }
    }
    return
}

if ([string]::IsNullOrWhiteSpace($ExePath) -or -not (Test-Path $ExePath)) {
    throw 'SpriteForge.App.exe not found. Pass -ExePath <path to SpriteForge.App.exe>.'
}

$ExePath = (Resolve-Path $ExePath).Path
$WorkDir = Split-Path -Parent $ExePath
$IconPath = Join-Path $WorkDir 'Assets\SpriteForge.ico'
if (-not (Test-Path $IconPath)) { $IconPath = $ExePath }

function New-Shortcut([string]$LinkPath) {
    $Shell = New-Object -ComObject WScript.Shell
    $Link = $Shell.CreateShortcut($LinkPath)
    $Link.TargetPath = $ExePath
    $Link.WorkingDirectory = $WorkDir
    $Link.IconLocation = "$IconPath,0"
    $Link.Description = 'SpriteForge - sprite sheet pipeline'
    $Link.Save()
    Write-Host "Created $LinkPath"
}

New-Shortcut $StartMenuLink
if ($Desktop) { New-Shortcut $DesktopLink }
