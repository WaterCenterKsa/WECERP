$ErrorActionPreference = "Stop"

# WEC ERP development processes that may keep build outputs locked.
$processes = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -eq "WecErp.Api.exe" -or
        ($_.Name -eq "dotnet.exe" -and $_.CommandLine -match "(?i)WecErp[\\/]Api")
    }

if (-not $processes) {
    Write-Host "No running WEC ERP API process was found."
    exit 0
}

foreach ($process in $processes) {
    Write-Host "Stopping WEC ERP API process $($process.ProcessId) ($($process.Name))..."

    try {
        Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
    }
    catch {
        Write-Warning "Failed to stop process $($process.ProcessId): $($_.Exception.Message)"
    }
}

Start-Sleep -Milliseconds 500

# Verify that the matching processes are gone before MSBuild tries to copy DLLs.
$remaining = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -eq "WecErp.Api.exe" -or
        ($_.Name -eq "dotnet.exe" -and $_.CommandLine -match "(?i)WecErp[\\/]Api")
    }

if ($remaining) {
    $ids = ($remaining | ForEach-Object ProcessId) -join ", "
    throw "WEC ERP API process(es) are still running: $ids"
}

Write-Host "WEC ERP API process cleanup complete."
