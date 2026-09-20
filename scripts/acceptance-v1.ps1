param(
    [switch]$SetupWorker
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$VerifyPrereqs = Join-Path $Root 'scripts\verify-prereqs.ps1'
$NuGetConfig = Join-Path $Root 'NuGet.config'

function Invoke-DotNetChecked {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    & dotnet @Arguments
    $ExitCode = $LASTEXITCODE
    if ($ExitCode -ne 0) {
        throw "$Description failed with exit code $ExitCode."
    }
}

$TestProjects = @(
    'tests\SpriteForge.Core.Tests\SpriteForge.Core.Tests.csproj',
    'tests\SpriteForge.Application.Tests\SpriteForge.Application.Tests.csproj',
    'tests\SpriteForge.Infrastructure.Tests\SpriteForge.Infrastructure.Tests.csproj',
    'tests\SpriteForge.Architecture.Tests\SpriteForge.Architecture.Tests.csproj',
    'tests\SpriteForge.Pipeline.Tests\SpriteForge.Pipeline.Tests.csproj'
)

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

    if (-not (Test-Path $NuGetConfig)) {
        throw "NuGet configuration is missing: $NuGetConfig"
    }

    Write-Host "== Restore =="
    Invoke-DotNetChecked -Arguments @('restore', 'SpriteForge.sln', '--configfile', $NuGetConfig, '-p:Platform=x64') -Description 'Solution restore'

    Write-Host "== Release x64 build =="
    Invoke-DotNetChecked -Arguments @('build', 'SpriteForge.sln', '-c', 'Release', '-p:Platform=x64', '--no-restore') -Description 'Release x64 build'

    Write-Host "== Automated acceptance tests =="
    foreach ($Project in $TestProjects) {
        Write-Host "-- $Project"
        Invoke-DotNetChecked -Arguments @('test', $Project, '-c', 'Release', '--no-restore', '--verbosity', 'normal') -Description "Tests for $Project"
    }

    Write-Host ""
    Write-Host "Automated V1 acceptance passed."
    Write-Host "Complete docs\V1-ACCEPTANCE.md manual desktop checks before labelling a release candidate desktop-accepted."
}
finally {
    Pop-Location
}
