$ErrorActionPreference = 'Continue'

function Test-Command($Name, $Args) {
    Write-Host "`n== $Name =="
    try { & $Name @Args | Select-Object -First 3 } catch { Write-Host "Missing or unavailable: $($_.Exception.Message)" }
}

Test-Command 'dotnet' @('--version')
Test-Command 'ffmpeg' @('-version')
Test-Command 'python' @('--version')

$WorkerPython = Join-Path $env:LOCALAPPDATA 'SpriteForge\worker\.venv\Scripts\python.exe'
Write-Host "`n== background-removal worker =="
if (Test-Path $WorkerPython) {
    try { & $WorkerPython -c 'import rembg, PIL; print("worker dependencies OK")' } catch { Write-Host "Worker dependency check failed: $($_.Exception.Message)" }
} else {
    Write-Host 'Worker environment is not installed. Run .\scripts\setup-worker.ps1'
}
