$ErrorActionPreference = "Stop"

$candidatePaths = @(
    (Join-Path $PSScriptRoot "appsettings.Production.json"),
    (Join-Path $PSScriptRoot "appsettings.json"),
    (Join-Path (Split-Path -Parent $PSScriptRoot) "src\DashboardCompras\appsettings.Production.json"),
    (Join-Path (Split-Path -Parent $PSScriptRoot) "src\DashboardCompras\appsettings.json")
)

$appSettingsPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $appSettingsPath) {
    throw "No se encontró un archivo appsettings para obtener el puerto del dashboard."
}

$config = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
$port = [int]$config.ServidorWeb.Puerto
$ruleName = "DashboardComprasLAN-$port"

if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow | Out-Null
    Write-Host "Regla creada para abrir el puerto $port en Firewall."
    Write-Host "Configuración leída desde: $appSettingsPath"
}
else {
    Write-Host "La regla $ruleName ya existe."
    Write-Host "Configuración leída desde: $appSettingsPath"
}
