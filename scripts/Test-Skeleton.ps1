#requires -Version 7.3
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$logDirectory = Join-Path $repoRoot 'artifacts/skeleton-smoke'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null

$hostsToCheck = @(
    @{ Name = 'Identity'; Project = 'src/Services/Identity/Identity.Api'; Port = 5210; OpenApi = $true },
    @{ Name = 'Catalog'; Project = 'src/Services/Catalog/Catalog.Api'; Port = 5220; OpenApi = $true },
    @{ Name = 'Inventory'; Project = 'src/Services/Inventory/Inventory.Api'; Port = 5230; OpenApi = $true },
    @{ Name = 'Ordering'; Project = 'src/Services/Ordering/Ordering.Api'; Port = 5240; OpenApi = $true },
    @{ Name = 'Notification'; Project = 'src/Services/Notification/Notification.Service'; Port = 5250 },
    @{ Name = 'Gateway'; Project = 'src/Gateway/MicroShop.Gateway'; Port = 5200 },
    @{ Name = 'Web'; Project = 'src/Web/MicroShop.Web'; Port = 5100 }
)

foreach ($hostToCheck in $hostsToCheck) {
    $port = $hostToCheck.Port
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
    try {
        $listener.Start()
    }
    catch {
        throw "Port $port is occupied. Stop the existing host before running this smoke check."
    }
    finally {
        $listener.Stop()
    }

    $stdout = Join-Path $logDirectory "$($hostToCheck.Name).stdout.log"
    $stderr = Join-Path $logDirectory "$($hostToCheck.Name).stderr.log"
    $previousDatabase = $env:ConnectionStrings__Database
    try {
        if ($hostToCheck.Name -in @('Catalog', 'Inventory')) {
            & "$PSScriptRoot/Set-ServiceEnvironment.ps1" -Service $hostToCheck.Name
        }
        $process = Start-Process -FilePath 'dotnet' -ArgumentList @(
            'run', '--project', $hostToCheck.Project, '--no-build', '--no-restore', '--launch-profile', 'http'
        ) -WorkingDirectory $repoRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    }
    finally { $env:ConnectionStrings__Database = $previousDatabase }

    try {
        $url = "http://localhost:$port"
        $response = $null
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        while ([DateTime]::UtcNow -lt $deadline) {
            if ($process.HasExited) {
                throw "$($hostToCheck.Name) exited. See $stdout and $stderr."
            }
            try {
                $response = Invoke-WebRequest -Uri $url -TimeoutSec 2
                break
            }
            catch {
                Start-Sleep -Milliseconds 200
            }
        }
        if ($null -eq $response -or $response.StatusCode -ne 200) {
            throw "$($hostToCheck.Name) did not become available. See $stdout and $stderr."
        }

        if ($hostToCheck.Name -eq 'Web') {
            if ($response.Content -notmatch 'blazor.web' -or $response.Content -notmatch 'webassembly') {
                throw 'Web response is missing its Blazor WebAssembly bootstrap.'
            }
            $script = Invoke-WebRequest -Uri "$url/_framework/blazor.web.js" -TimeoutSec 5
            if ($script.StatusCode -ne 200 -or $script.RawContentLength -lt 1000) {
                throw 'Blazor boot script is unavailable.'
            }
        }
        else {
            $identity = $response.Content | ConvertFrom-Json
            $expectedPhase = if ($hostToCheck.Name -in @('Catalog', 'Inventory')) { $hostToCheck.Name } else { 'Solution skeleton' }
            if ($identity.service -ne $hostToCheck.Name -or $identity.phase -ne $expectedPhase) {
                throw "Unexpected identity response on port $port."
            }
        }

        if ($hostToCheck.OpenApi) {
            $openApi = Invoke-RestMethod -Uri "$url/openapi/v1.json" -TimeoutSec 5
            if (-not $openApi.openapi -or -not $openApi.paths.'/'.get) {
                throw "$($hostToCheck.Name) OpenAPI does not describe the root endpoint."
            }
        }
        if ($hostToCheck.Name -eq 'Gateway') {
            $unmapped = Invoke-WebRequest -Uri "$url/api/catalog/products" -SkipHttpErrorCheck -TimeoutSec 5
            if ($unmapped.StatusCode -ne 404) {
                throw 'Gateway unexpectedly has a business route before Phase 7.'
            }
        }
        if ($hostToCheck.Name -eq 'Catalog') {
            $products = Invoke-RestMethod -Uri "$url/api/catalog/products?pageSize=2" -TimeoutSec 5
            if ($products.pageSize -ne 2 -or $null -eq $products.totalCount) {
                throw 'Catalog did not return a paginated database result.'
            }
            $swagger = Invoke-WebRequest -Uri "$url/swagger/index.html" -TimeoutSec 5
            if ($swagger.Content -notmatch 'swagger-ui') { throw 'Catalog Swagger UI is unavailable.' }
        }
        if ($hostToCheck.Name -eq 'Inventory') {
            $items = Invoke-RestMethod -Uri "$url/api/inventory/items?pageSize=2" -TimeoutSec 5
            if ($items.pageSize -ne 2 -or $null -eq $items.totalCount) {
                throw 'Inventory did not return a paginated database result.'
            }
            foreach ($item in $items.items) {
                if ($item.available -ne ($item.onHand - $item.reserved) -or $item.available -lt 0) {
                    throw 'Inventory returned inconsistent quantities.'
                }
            }
            $swagger = Invoke-WebRequest -Uri "$url/swagger/index.html" -TimeoutSec 5
            if ($swagger.Content -notmatch 'swagger-ui') { throw 'Inventory Swagger UI is unavailable.' }
        }

        Write-Output "PASS $($hostToCheck.Name) ($url)"
    }
    finally {
        if (-not $process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
    }
}

Write-Output 'Seven hosts passed, including Catalog/Inventory database reads and Swagger delivery. Browser execution is not covered.'
