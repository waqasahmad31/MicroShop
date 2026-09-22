#requires -Version 7.3
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$previousDatabase = $env:ConnectionStrings__Database
$previousTestDatabase = $env:CATALOG_TEST_CONNECTION_STRING
try {
    & "$PSScriptRoot/Set-ServiceEnvironment.ps1" -Service Catalog
    $env:CATALOG_TEST_CONNECTION_STRING = $env:ConnectionStrings__Database
    dotnet test "$repoRoot/MicroShop.sln" --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Catalog/solution tests failed.' }
}
finally {
    $env:ConnectionStrings__Database = $previousDatabase
    $env:CATALOG_TEST_CONNECTION_STRING = $previousTestDatabase
}
