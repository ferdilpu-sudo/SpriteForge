param(
    [switch]$Strict,
    [switch]$RequireWorker
)

$ErrorActionPreference = 'Continue'
$Failures = [System.Collections.Generic.List[string]]::new()

function Add-RuntimeFailure {
    param([string]$Message)
    $script:Failures.Add($Message) | Out-Null
    Write-Host "FAILED: $Message"
}

function Test-RuntimeCommand {
    param(
        [string]$Name,
        [string[]]$Arguments
    )

    Write-Host "`n== $Name =="
    $Command = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $Command) {
        Add-RuntimeFailure "$Name was not found on PATH."
        return $false
    }

    try {
        $Output = & $Name @Arguments 2>&1
        $ExitCode = $LASTEXITCODE
        $Output | Select-Object -First 3
        if ($ExitCode -ne 0) {
            Add-RuntimeFailure "$Name exited with code $ExitCode."
            return $false
        }
        return $true
    }
    catch {
        Add-RuntimeFailure "$Name is unavailable: $($_.Exception.Message)"
        return $false
    }
}

$null = Test-RuntimeCommand 'ffmpeg' @('-version')
$null = Test-RuntimeCommand 'python' @('--version')

$WorkerPython = Join-Path $env:LOCALAPPDATA 'SpriteForge\worker\.venv\Scripts\python.exe'
Write-Host "`n== background-removal worker =="
if (Test-Path $WorkerPython) {
    try {
        $WorkerOutput = & $WorkerPython -c 'import rembg, PIL' 2>&1
        $WorkerExitCode = $LASTEXITCODE
        $WorkerOutput | Select-Object -First 3
        if ($WorkerExitCode -eq 0) {
            Write-Host 'worker dependencies OK'
        } elseif ($RequireWorker) {
            Add-RuntimeFailure 'Background-removal worker dependencies are not healthy.'
        } else {
            Write-Host "Worker dependency check failed with code $WorkerExitCode."
        }
    }
    catch {
        if ($RequireWorker) {
            Add-RuntimeFailure 'Background-removal worker dependencies are not healthy.'
        } else {
            Write-Host "Worker dependency check failed: $($_.Exception.Message)"
        }
    }
} elseif ($RequireWorker) {
    Add-RuntimeFailure 'Background-removal worker is not installed. Run .\scripts\setup-worker.ps1.'
} else {
    Write-Host 'Worker environment is optional until Cutout is used. Run .\scripts\setup-worker.ps1 to install it.'
}

if ($Strict -and $Failures.Count -gt 0) {
    Write-Host "`nRuntime prerequisite validation failed:"
    foreach ($Failure in $Failures) {
        Write-Host " - $Failure"
    }
    throw "SpriteForge runtime prerequisite validation failed with $($Failures.Count) issue(s)."
}

if ($Strict) {
    Write-Host "`nRuntime prerequisite validation passed."
}
