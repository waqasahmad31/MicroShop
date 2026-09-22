#requires -Version 7.3
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Identity', 'Catalog', 'Inventory', 'Ordering')]
    [string] $Service
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    # Read Compose's merged settings privately so .env quoting/overrides have the same meaning here.
    $json = docker compose config --format json
    if ($LASTEXITCODE -ne 0) { throw 'Create .env and validate Compose configuration first.' }
    $config = ($json -join [Environment]::NewLine) | ConvertFrom-Json
    $postgres = $config.services.postgres
    $name = $Service.ToLowerInvariant()
    $passwordName = $Service.ToUpperInvariant() + '_DB_PASSWORD'
    $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
    $connection['Host'] = '127.0.0.1'
    $connection['Port'] = [string]$postgres.ports[0].published
    $connection['Database'] = $name + '_db'
    $connection['Username'] = $name + '_app'
    $connection['Password'] = $postgres.environment.$passwordName
    $env:ConnectionStrings__Database = $connection.ConnectionString
    Write-Output "Set ConnectionStrings__Database for $Service in this terminal. No connection opened; value not printed."
}
finally { Pop-Location }
