param(
    [string]$Version = '0.9.0-beta.1',
    [string]$Channel = 'beta',
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$OutputRoot = '',
    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot

if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a valid semantic version."
}
if ($Channel -notin @('beta', 'stable')) {
    throw "Unsupported channel '$Channel'. Supported channels: beta, stable."
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $Root 'artifacts'
} elseif (-not [System.IO.Path]::IsPathRooted($OutputRoot)) {
    $OutputRoot = Join-Path $Root $OutputRoot
}

$RunningSpriteForge = @(Get-Process -Name 'SpriteForge.App' -ErrorAction SilentlyContinue)
if ($RunningSpriteForge.Count -gt 0) {
    $Ids = ($RunningSpriteForge | ForEach-Object { $_.Id }) -join ', '
    throw "SpriteForge is running (PID: $Ids). Close it before packaging."
}

$Platform = switch ($Runtime) {
    'win-x64' { 'x64' }
    'win-arm64' { 'ARM64' }
    default { throw "Unsupported runtime '$Runtime'. Supported runtimes: win-x64, win-arm64." }
}

$PackageName = "SpriteForge-$Version-$Runtime"
$PackageRoot = Join-Path $OutputRoot $PackageName
$ZipPath = Join-Path $OutputRoot "$PackageName.zip"
$HashPath = "$ZipPath.sha256"

foreach ($Path in @($PackageRoot, $ZipPath, $HashPath)) {
    if (Test-Path $Path) {
        Remove-Item -Recurse -Force $Path
    }
}

New-Item -ItemType Directory -Force -Path $PackageRoot | Out-Null

Write-Host "== Publishing SpriteForge $Version ($Runtime, self-contained) =="
dotnet publish (Join-Path $Root 'src\SpriteForge.App\SpriteForge.App.csproj') `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:Platform=$Platform `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishTrimmed=false `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o $PackageRoot

$ExePath = Join-Path $PackageRoot 'SpriteForge.App.exe'
$PriPath = Join-Path $PackageRoot 'SpriteForge.App.pri'
$WorkerPath = Join-Path $PackageRoot 'workers\background-removal\worker.py'
if (-not (Test-Path $ExePath)) { throw "Published executable is missing: $ExePath" }
if (-not (Test-Path $PriPath)) { throw "Published app PRI is missing: $PriPath" }
if (-not (Test-Path $WorkerPath)) { throw "Background-removal worker source is missing: $WorkerPath" }

$ScriptsDirectory = Join-Path $PackageRoot 'scripts'
$DocsDirectory = Join-Path $PackageRoot 'docs'
New-Item -ItemType Directory -Force -Path $ScriptsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $DocsDirectory | Out-Null

Copy-Item (Join-Path $Root 'scripts\verify-runtime-prereqs.ps1') (Join-Path $ScriptsDirectory 'verify-runtime-prereqs.ps1')
Copy-Item (Join-Path $Root 'scripts\setup-worker.ps1') (Join-Path $ScriptsDirectory 'setup-worker.ps1')
Copy-Item (Join-Path $Root 'scripts\desktop-smoke.ps1') (Join-Path $ScriptsDirectory 'desktop-smoke.ps1')
Copy-Item (Join-Path $Root 'docs\V1-ACCEPTANCE.md') (Join-Path $DocsDirectory 'V1-ACCEPTANCE.md')
Copy-Item (Join-Path $Root 'README.md') (Join-Path $PackageRoot 'README.md')
Copy-Item (Join-Path $Root 'CHANGELOG.md') (Join-Path $PackageRoot 'CHANGELOG.md')
Copy-Item (Join-Path $Root 'THIRD-PARTY-NOTICES.md') (Join-Path $PackageRoot 'THIRD-PARTY-NOTICES.md')

$SourceCommit = $env:GITHUB_SHA
if ([string]::IsNullOrWhiteSpace($SourceCommit)) {
    $GitCommit = & git -C $Root rev-parse HEAD 2>$null
    $SourceCommit = if ($LASTEXITCODE -eq 0 -and $GitCommit) { $GitCommit.Trim() } else { 'unknown' }
}

$BuildInfo = [ordered]@{
    product = 'SpriteForge'
    version = $Version
    channel = $Channel
    sourceCommit = $SourceCommit
    configuration = $Configuration
    runtime = $Runtime
    selfContained = $true
    externalRuntimePrerequisites = @('FFmpeg', 'Python 3.11+ for Cutout')
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
}
$BuildInfo | ConvertTo-Json | Set-Content -Path (Join-Path $PackageRoot 'BUILD-INFO.json') -Encoding UTF8

$ReleaseReadme = @"
# SpriteForge $Version

This is an unpackaged, self-contained WinUI 3 $Runtime build.

The .NET runtime and Windows App SDK are included. You do not need the .NET SDK to run SpriteForge.

## Before first launch

1. Install FFmpeg and ensure `ffmpeg` is on PATH.
2. Install Python 3.11+ if you plan to use Cutout.
3. From this package directory, run:

```powershell
.\scripts\setup-worker.ps1
.\scripts\verify-runtime-prereqs.ps1 -Strict -RequireWorker
```

4. Run the startup smoke check:

```powershell
.\scripts\desktop-smoke.ps1
```

The background-removal model may download data on first use through rembg.
"@
Set-Content -Path (Join-Path $PackageRoot 'RELEASE-README.md') -Value $ReleaseReadme -Encoding UTF8

$FileCount = (Get-ChildItem -Path $PackageRoot -Recurse -File).Count
$TotalBytes = (Get-ChildItem -Path $PackageRoot -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host "Published package: $PackageRoot"
Write-Host "Files: $FileCount"
Write-Host ("Size: {0:N2} MB" -f ($TotalBytes / 1MB))

if (-not $NoZip) {
    Write-Host "== Creating release ZIP =="
    Compress-Archive -Path (Join-Path $PackageRoot '*') -DestinationPath $ZipPath -CompressionLevel Optimal
    $Hash = (Get-FileHash -Algorithm SHA256 -Path $ZipPath).Hash.ToLowerInvariant()
    "$Hash  $([System.IO.Path]::GetFileName($ZipPath))" | Set-Content -Path $HashPath -Encoding ascii
    Write-Host "ZIP: $ZipPath"
    Write-Host "SHA-256: $Hash"
    Write-Host "Checksum file: $HashPath"
}
