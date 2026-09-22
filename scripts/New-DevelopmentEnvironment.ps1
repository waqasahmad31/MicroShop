#requires -Version 7.3
[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int] $PostgresPort = 5432
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $repoRoot '.env'
if (Test-Path -LiteralPath $destination) {
    throw '.env already exists; edit it locally instead of overwriting existing credentials.'
}

$content = [IO.File]::ReadAllText((Join-Path $repoRoot '.env.example'))
$passwordNames = @('POSTGRES_ADMIN_PASSWORD', 'IDENTITY_DB_PASSWORD', 'CATALOG_DB_PASSWORD',
    'INVENTORY_DB_PASSWORD', 'ORDERING_DB_PASSWORD', 'RABBITMQ_PASSWORD')
foreach ($name in $passwordNames) {
    $password = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $content = [regex]::Replace($content, "(?m)^$name=.*$", "$name=$password")
}
$content = [regex]::Replace($content, '(?m)^POSTGRES_PORT=.*$', "POSTGRES_PORT=$PostgresPort")
[IO.File]::WriteAllText($destination, $content, [Text.UTF8Encoding]::new($false))
Write-Output "Created ignored .env with distinct random development passwords; PostgreSQL host port $PostgresPort."
