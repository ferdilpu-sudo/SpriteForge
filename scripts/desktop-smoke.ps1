param(
    [string]$PackageRoot = '',
    [int]$StartupSeconds = 8,
    [switch]$KeepRunning
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
    if (Test-Path (Join-Path $Root 'SpriteForge.App.exe')) {
        $PackageRoot = $Root
    } else {
        $ArtifactsRoot = Join-Path $Root 'artifacts'
        $Candidate = Get-ChildItem -Path $ArtifactsRoot -Directory -Filter 'SpriteForge-*-win-x64' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if (-not $Candidate) {
            throw "No packaged SpriteForge win-x64 build was found under $ArtifactsRoot. Run .\scripts\package-rc.ps1 first."
        }
        $PackageRoot = $Candidate.FullName
    }
} elseif (-not [System.IO.Path]::IsPathRooted($PackageRoot)) {
    $PackageRoot = Join-Path $Root $PackageRoot
}

$PackageRoot = [System.IO.Path]::GetFullPath($PackageRoot)
$ExePath = Join-Path $PackageRoot 'SpriteForge.App.exe'
if (-not (Test-Path $ExePath)) {
    throw "SpriteForge executable was not found at $ExePath."
}

$Existing = @(Get-Process -Name 'SpriteForge.App' -ErrorAction SilentlyContinue)
if ($Existing.Count -gt 0) {
    $Ids = ($Existing | ForEach-Object { $_.Id }) -join ', '
    throw "SpriteForge is already running (PID: $Ids). Close it before running a clean startup smoke test."
}

$VerifyScript = Join-Path $PackageRoot 'scripts\verify-runtime-prereqs.ps1'
if (Test-Path $VerifyScript) {
    Write-Host "== Verifying packaged runtime prerequisites =="
    & $VerifyScript -Strict -RequireWorker
}

$Process = $null
try {
    Write-Host "Launching SpriteForge..."
    $Process = Start-Process -FilePath $ExePath -WorkingDirectory $PackageRoot -PassThru
    Start-Sleep -Seconds $StartupSeconds
    $Process.Refresh()

    if ($Process.HasExited) {
        $HexCode = ('0x{0:X8}' -f [int]$Process.ExitCode)
        $StartupLog = Join-Path $env:LOCALAPPDATA 'SpriteForge\logs\startup.log'
        Write-Host "SpriteForge exited during startup. Exit code: $($Process.ExitCode) ($HexCode)."
        if (Test-Path $StartupLog) {
            Write-Host "== Startup log tail =="
            Get-Content $StartupLog -Tail 40
        }
        throw "SpriteForge exited during the startup smoke window with code $($Process.ExitCode) ($HexCode)."
    }

    Write-Host "Startup smoke passed: SpriteForge is still running after $StartupSeconds seconds."
    if ($KeepRunning) {
        Write-Host "SpriteForge remains open because -KeepRunning was requested."
        Write-Host "Continue the interactive checks in docs\V1-ACCEPTANCE.md."
    }
}
finally {
    if ($Process -and -not $Process.HasExited -and -not $KeepRunning) {
        Write-Host "Stopping smoke-test process..."
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit()
        Write-Host "Smoke-test process stopped cleanly."
    }
}
