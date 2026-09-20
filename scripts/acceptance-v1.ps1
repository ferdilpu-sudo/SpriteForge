param(
    [switch]$SetupWorker
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$VerifyPrereqs = Join-Path $Root 'scripts\verify-prereqs.ps1'

Push-Location $Root
try {
    Write-Host "== SpriteForge V1 prerequisite check =="
    & $VerifyPrereqs -Strict

    if ($SetupWorker) {
        Write-Host "== Setting up local background-removal worker =="
        & (Join-Path $Root 'scripts\setup-worker.ps1')

        Write-Host "== Verifying background-removal worker =="
        & $VerifyPrereqs -Strict -RequireWorker
    }

    Write-Host "== Restore =="
    dotnet restore SpriteForge.sln

    Write-Host "== Release x64 build =="
    dotnet build SpriteForge.sln -c Release -p:Platform=x64 --no-restore

    Write-Host "== Automated acceptance tests =="
    dotnet test SpriteForge.sln -c Release --no-restore --verbosity normal

    Write-Host ""
    Write-Host "Automated V1 acceptance passed."
    Write-Host "Complete docs\V1-ACCEPTANCE.md manual desktop checks before labelling a release candidate desktop-accepted."
}
finally {
    Pop-Location
}
