param(
    [string]$BaseUrl = "http://localhost:5048",
    [string]$AdminEmail = "admin@iras.local",
    [string]$AdminPassword = "ChangeMe@123"
)

$ErrorActionPreference = "Stop"

function Invoke-Check {
    param(
        [string]$Name,
        [scriptblock]$Call
    )

    try {
        & $Call | Out-Null
        [pscustomobject]@{ Check = $Name; Status = "PASS" }
    }
    catch {
        [pscustomobject]@{ Check = $Name; Status = "FAIL"; Error = $_.Exception.Message }
    }
}

$results = @()
$results += Invoke-Check "Swagger JSON" {
    Invoke-WebRequest -Uri "$BaseUrl/swagger/v1/swagger.json" -UseBasicParsing -TimeoutSec 15
}

$token = $null
$results += Invoke-Check "Admin Login" {
    $body = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
    $login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 20
    $script:token = $login.token
}

if ($token) {
    $headers = @{ Authorization = "Bearer $token" }
    foreach ($endpoint in @(
        "/api/auth/me",
        "/api/skills",
        "/api/skill-resources",
        "/api/admin/system/ai-status",
        "/api/admin/system/settings",
        "/api/admin/reports/dashboard",
        "/api/admin/users",
        "/api/admin/jobs",
        "/api/admin/audit-logs",
        "/api/admin/knowledge-base"
    )) {
        $results += Invoke-Check $endpoint {
            Invoke-WebRequest -Uri "$BaseUrl$endpoint" -Headers $headers -UseBasicParsing -TimeoutSec 20
        }
    }
}

$results | Format-Table -AutoSize

if ($results.Status -contains "FAIL") {
    exit 1
}
