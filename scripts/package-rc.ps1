param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$OutputRoot = '',
    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $Root 'artifacts'
} elseif (-not [System.IO.Path]::IsPathRooted($OutputRoot)) {
    $OutputRoot = Join-Path $Root $OutputRoot
}

$RunningSpriteForge = @(Get-Process -Name 'SpriteForge.App' -ErrorAction SilentlyContinue)
if ($RunningSpriteForge.Count -gt 0) {
    $Ids = ($RunningSpriteForge | ForEach-Object { $_.Id }) -join ', '
    throw "SpriteForge is running (PID: $Ids). Close it before rebuilding the RC package."
}
$PackageName = "SpriteForge-v1-rc-$Runtime"
$PackageRoot = Join-Path $OutputRoot $PackageName
$ZipPath = Join-Path $OutputRoot "$PackageName.zip"

if (Test-Path $PackageRoot) {
    Remove-Item -Recurse -Force $PackageRoot
}
if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}

New-Item -ItemType Directory -Force -Path $PackageRoot | Out-Null

Write-Host "== Publishing SpriteForge ($Runtime, self-contained) =="
dotnet publish (Join-Path $Root 'src\SpriteForge.App\SpriteForge.App.csproj') `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:Platform=x64 `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishTrimmed=false `
    -p:PublishSingleFile=false `
    -o $PackageRoot

$ExePath = Join-Path $PackageRoot 'SpriteForge.App.exe'
$WorkerPath = Join-Path $PackageRoot 'workers\background-removal\worker.py'
if (-not (Test-Path $ExePath)) {
    throw "Published executable is missing: $ExePath"
}
if (-not (Test-Path $WorkerPath)) {
    throw "Background-removal worker source is missing from publish output: $WorkerPath"
}

$ScriptsDirectory = Join-Path $PackageRoot 'scripts'
$DocsDirectory = Join-Path $PackageRoot 'docs'
New-Item -ItemType Directory -Force -Path $ScriptsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $DocsDirectory | Out-Null

Copy-Item (Join-Path $Root 'scripts\verify-prereqs.ps1') (Join-Path $ScriptsDirectory 'verify-prereqs.ps1')
Copy-Item (Join-Path $Root 'scripts\setup-worker.ps1') (Join-Path $ScriptsDirectory 'setup-worker.ps1')
Copy-Item (Join-Path $Root 'scripts\desktop-smoke.ps1') (Join-Path $ScriptsDirectory 'desktop-smoke.ps1')
Copy-Item (Join-Path $Root 'docs\V1-ACCEPTANCE.md') (Join-Path $DocsDirectory 'V1-ACCEPTANCE.md')
Copy-Item (Join-Path $Root 'README.md') (Join-Path $PackageRoot 'README.md')

$SourceCommit = $env:GITHUB_SHA
if ([string]::IsNullOrWhiteSpace($SourceCommit)) {
    $GitCommit = & git -C $Root rev-parse HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($GitCommit)) {
        $SourceCommit = $GitCommit.Trim()
    } else {
        $SourceCommit = 'unknown'
    }
}

$BuildInfo = [ordered]@{
    product = 'SpriteForge'
    channel = 'v1-rc'
    sourceCommit = $SourceCommit
    configuration = $Configuration
    runtime = $Runtime
    selfContained = $true
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
}
$BuildInfo | ConvertTo-Json | Set-Content -Path (Join-Path $PackageRoot 'RC-BUILD.json') -Encoding UTF8

$RcReadme = @'
# SpriteForge V1 Release Candidate

This is an unpackaged, self-contained WinUI 3 x64 test build.

## Before first launch

1. Install FFmpeg and ensure `ffmpeg` is available on PATH.
2. Install Python 3.11+.
3. From this package directory, run:

```powershell
.\scripts\setup-worker.ps1
.\scripts\verify-prereqs.ps1 -Strict -RequireWorker
```

4. Run the startup smoke check:

```powershell
.\scripts\desktop-smoke.ps1
```

5. Complete `docs\V1-ACCEPTANCE.md`.

The app runtime is self-contained, but FFmpeg and the local Python/rembg worker remain external runtime prerequisites for media processing.
'@
Set-Content -Path (Join-Path $PackageRoot 'RC-README.md') -Value $RcReadme -Encoding UTF8

$FileCount = (Get-ChildItem -Path $PackageRoot -Recurse -File).Count
$TotalBytes = (Get-ChildItem -Path $PackageRoot -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host "Published package: $PackageRoot"
Write-Host "Files: $FileCount"
Write-Host ("Size: {0:N2} MB" -f ($TotalBytes / 1MB))

if (-not $NoZip) {
    Write-Host "== Creating RC ZIP =="
    Compress-Archive -Path (Join-Path $PackageRoot '*') -DestinationPath $ZipPath -CompressionLevel Optimal
    $Hash = (Get-FileHash -Algorithm SHA256 -Path $ZipPath).Hash.ToLowerInvariant()
    Write-Host "ZIP: $ZipPath"
    Write-Host "SHA-256: $Hash"
}