$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$WorkerSource = Join-Path $Root 'workers\background-removal'
$RuntimeRoot = Join-Path $env:LOCALAPPDATA 'SpriteForge\worker'
$Venv = Join-Path $RuntimeRoot '.venv'

New-Item -ItemType Directory -Force -Path $RuntimeRoot | Out-Null
python -m venv $Venv
$Python = Join-Path $Venv 'Scripts\python.exe'
& $Python -m pip install --upgrade pip
& $Python -m pip install -r (Join-Path $WorkerSource 'requirements.txt')
Write-Host "Background-removal worker ready: $Python"
