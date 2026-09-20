param(
    [switch]$Strict,
    [switch]$RequireWorker
)

$ErrorActionPreference = 'Continue'
$Failures = [System.Collections.Generic.List[string]]::new()

function Add-PrerequisiteFailure {
    param([string]$Message)
    $script:Failures.Add($Message) | Out-Null
    Write-Host "FAILED: $Message"
}

function Test-ExternalCommand {
    param(
        [string]$Name,
        [string[]]$Arguments
    )

    Write-Host "`n== $Name =="
    $Command = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $Command) {
        Add-PrerequisiteFailure "$Name was not found on PATH."
        return $false
    }

    try {
        $Output = & $Name @Arguments 2>&1
        $ExitCode = $LASTEXITCODE
        $Output | Select-Object -First 3
        if ($ExitCode -ne 0) {
            Add-PrerequisiteFailure "$Name exited with code $ExitCode."
            return $false
        }
        return $true
    }
    catch {
        Add-PrerequisiteFailure "$Name is unavailable: $($_.Exception.Message)"
        return $false
    }
}

$DotnetReady = Test-ExternalCommand 'dotnet' @('--version')
$FfmpegReady = Test-ExternalCommand 'ffmpeg' @('-version')
$PythonReady = Test-ExternalCommand 'python' @('--version')

$WorkerPython = Join-Path $env:LOCALAPPDATA 'SpriteForge\worker\.venv\Scripts\python.exe'
Write-Host "`n== background-removal worker =="
$WorkerReady = $false
if (Test-Path $WorkerPython) {
    try {
        $WorkerOutput = & $WorkerPython -c 'import rembg, PIL' 2>&1
        $WorkerExitCode = $LASTEXITCODE
        $WorkerOutput | Select-Object -First 3
        if ($WorkerExitCode -eq 0) {
            $WorkerReady = $true
            Write-Host 'worker dependencies OK'
        } else {
            Write-Host "Worker dependency check failed with code $WorkerExitCode."
            if ($RequireWorker) {
                Add-PrerequisiteFailure 'Background-removal worker dependencies are not healthy.'
            }
        }
    }
    catch {
        Write-Host "Worker dependency check failed: $($_.Exception.Message)"
        if ($RequireWorker) {
            Add-PrerequisiteFailure 'Background-removal worker dependencies are not healthy.'
        }
    }
} else {
    Write-Host 'Worker environment is not installed. Run .\scripts\setup-worker.ps1'
    if ($RequireWorker) {
        Add-PrerequisiteFailure 'Background-removal worker environment is not installed.'
    }
}

if ($Strict -and $Failures.Count -gt 0) {
    Write-Host "`nPrerequisite validation failed:"
    foreach ($Failure in $Failures) {
        Write-Host " - $Failure"
    }
    throw "SpriteForge prerequisite validation failed with $($Failures.Count) issue(s)."
}

if ($Strict) {
    Write-Host "`nStrict prerequisite validation passed."
}