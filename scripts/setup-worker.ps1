$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$WorkerSource = Join-Path $Root 'workers\background-removal'
$RuntimeRoot = Join-Path $env:LOCALAPPDATA 'SpriteForge\worker'
$Venv = Join-Path $RuntimeRoot '.venv'

function Invoke-NativeChecked {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    & $FilePath @Arguments
    $ExitCode = $LASTEXITCODE
    if ($ExitCode -ne 0) {
        throw "$Description failed with exit code $ExitCode."
    }
}

$SystemPython = Get-Command 'python' -ErrorAction SilentlyContinue
if (-not $SystemPython) {
    throw 'Python was not found on PATH. Install Python 3.11+ before setting up the background-removal worker.'
}

New-Item -ItemType Directory -Force -Path $RuntimeRoot | Out-Null

Invoke-NativeChecked -FilePath $SystemPython.Source -Arguments @('-m', 'venv', $Venv) -Description 'Creating the background-removal virtual environment'

$Python = Join-Path $Venv 'Scripts\python.exe'
if (-not (Test-Path $Python)) {
    throw "Worker Python executable was not created: $Python"
}

Invoke-NativeChecked -FilePath $Python -Arguments @('-m', 'pip', 'install', '--upgrade', 'pip') -Description 'Upgrading pip'
Invoke-NativeChecked -FilePath $Python -Arguments @('-m', 'pip', 'install', '-r', (Join-Path $WorkerSource 'requirements.txt')) -Description 'Installing background-removal dependencies'
Invoke-NativeChecked -FilePath $Python -Arguments @('-c', 'import rembg, PIL') -Description 'Validating background-removal dependencies'

Write-Host "Background-removal worker ready: $Python"
