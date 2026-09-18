$ErrorActionPreference = "Stop"

# Releases ports used by WEC ERP development without touching unrelated applications.
$ports = @(7000, 7001, 7010, 7011)
$connections = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $ports -contains $_.LocalPort } |
    Sort-Object OwningProcess, LocalPort -Unique

if (-not $connections) {
    Write-Host "No WEC ERP development ports are currently listening."
    exit 0
}

foreach ($connection in $connections) {
    $process = Get-CimInstance Win32_Process -Filter "ProcessId = $($connection.OwningProcess)" -ErrorAction SilentlyContinue

    if ($process -and (
        $process.Name -eq "WecErp.Api.exe" -or
        ($process.Name -eq "dotnet.exe" -and $process.CommandLine -match "WecErp.Api")
    )) {
        Write-Host "Stopping WEC ERP API process $($process.ProcessId) on port $($connection.LocalPort)..."
        Stop-Process -Id $process.ProcessId -Force
    }
    else {
        $name = if ($process) { $process.Name } else { "<unknown>" }
        Write-Warning "Port $($connection.LocalPort) is used by PID $($connection.OwningProcess) ($name), which does not appear to be WEC ERP. It was not stopped."
    }
}

Write-Host "WEC ERP API port cleanup complete."
