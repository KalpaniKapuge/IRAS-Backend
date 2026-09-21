param(
    [string]$BaseUrl = "http://localhost:5173",
    [string]$AdminEmail = "",
    [string]$AdminPassword = "",
    [switch]$ShowBrowser
)

$ErrorActionPreference = "Stop"

$env:IRAS_E2E_BASE_URL = $BaseUrl
$env:IRAS_E2E_ADMIN_EMAIL = $AdminEmail
$env:IRAS_E2E_ADMIN_PASSWORD = $AdminPassword
$env:IRAS_E2E_HEADLESS = if ($ShowBrowser) { "false" } else { "true" }

dotnet test .\IRAS.E2ETests\IRAS.E2ETests.csproj --logger "trx;LogFileName=iras-e2e.trx"
