#requires -Version 7.3
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$previousDatabase = $env:ConnectionStrings__Database
$previousCatalog = $env:CATALOG_TEST_CONNECTION_STRING
$previousInventory = $env:INVENTORY_TEST_CONNECTION_STRING
$previousOrdering = $env:ORDERING_TEST_CONNECTION_STRING
try {
    & "$PSScriptRoot/Set-ServiceEnvironment.ps1" -Service Catalog
    $env:CATALOG_TEST_CONNECTION_STRING = $env:ConnectionStrings__Database
    & "$PSScriptRoot/Set-ServiceEnvironment.ps1" -Service Inventory
    $env:INVENTORY_TEST_CONNECTION_STRING = $env:ConnectionStrings__Database
    & "$PSScriptRoot/Set-ServiceEnvironment.ps1" -Service Ordering
    $env:ORDERING_TEST_CONNECTION_STRING = $env:ConnectionStrings__Database
    # Each fixture overrides host configuration with its own service-specific connection.
    $env:ConnectionStrings__Database = $previousDatabase
    # Bound build concurrency as the solution grows; parallel MSBuild exhausted this machine's memory.
    dotnet test "$repoRoot/MicroShop.sln" --nologo --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Solution tests failed.' }
}
finally {
    $env:ConnectionStrings__Database = $previousDatabase
    $env:CATALOG_TEST_CONNECTION_STRING = $previousCatalog
    $env:INVENTORY_TEST_CONNECTION_STRING = $previousInventory
    $env:ORDERING_TEST_CONNECTION_STRING = $previousOrdering
}
