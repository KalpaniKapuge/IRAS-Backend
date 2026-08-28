# Frees port 5048 (kills any leftover IRAS.API.exe still holding it from a previous
# run that didn't shut down cleanly), then starts the API normally.
# Use this instead of `dotnet run` to avoid the recurring "address already in use" error.

$port = 5048
$conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
if ($conn) {
    $procId = $conn[0].OwningProcess
    Write-Host "Port $port is in use by PID $procId — stopping it first..." -ForegroundColor Yellow
    Stop-Process -Id $procId -Force
    Start-Sleep -Milliseconds 500
}

dotnet run --project "$PSScriptRoot\IRAS.API.csproj"
