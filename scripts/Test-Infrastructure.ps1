#requires -Version 7.3
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    docker compose config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Compose configuration is invalid.' }

    # Capture configuration privately; printing resolved Compose JSON would expose passwords.
    $json = docker compose config --format json
    if ($LASTEXITCODE -ne 0) { throw 'Could not read Compose configuration.' }
    $config = ($json -join [Environment]::NewLine) | ConvertFrom-Json
    if (@($config.services.PSObject.Properties).Count -ne 2) {
        throw 'Phase 2 expects only postgres and rabbitmq services.'
    }
    foreach ($service in @('postgres', 'rabbitmq')) {
        $container = docker compose ps -q $service
        if ($LASTEXITCODE -ne 0 -or -not $container) { throw "$service is not running." }
        $health = docker inspect --format '{{.State.Health.Status}}' $container
        if ($LASTEXITCODE -ne 0 -or $health -ne 'healthy') { throw "$service is not healthy yet. Run docker compose ps/logs." }
        Write-Output "PASS $service container healthy"
    }

    docker compose exec -T postgres bash /microshop-verify/verify-isolation.sh
    if ($LASTEXITCODE -ne 0) { throw 'Database authentication/isolation verification failed.' }

    $pgPort = [int]$config.services.postgres.ports[0].published
    $pgClient = [Net.Sockets.TcpClient]::new()
    try {
        $null = $pgClient.ConnectAsync('127.0.0.1', $pgPort).WaitAsync([TimeSpan]::FromSeconds(5)).GetAwaiter().GetResult()
        Write-Output "PASS PostgreSQL published port $pgPort reachable"
    }
    finally { $pgClient.Dispose() }

    $rabbit = $config.services.rabbitmq
    $amqpPort = [int]($rabbit.ports | Where-Object target -eq 5672).published
    $managementPort = [int]($rabbit.ports | Where-Object target -eq 15672).published
    $managementUrl = "http://127.0.0.1:$managementPort"
    $ui = Invoke-WebRequest -Uri "$managementUrl/" -TimeoutSec 10
    if ($ui.StatusCode -ne 200 -or $ui.Content -notmatch 'RabbitMQ Management') {
        throw 'RabbitMQ management UI did not return its HTML page.'
    }

    $pair = "$($rabbit.environment.RABBITMQ_DEFAULT_USER):$($rabbit.environment.RABBITMQ_DEFAULT_PASS)"
    $headers = @{ Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pair)) }
    $overview = Invoke-RestMethod -Uri "$managementUrl/api/overview" -Headers $headers -TimeoutSec 10
    $vhost = [Uri]::EscapeDataString($rabbit.environment.RABBITMQ_DEFAULT_VHOST)
    $user = [Uri]::EscapeDataString($rabbit.environment.RABBITMQ_DEFAULT_USER)
    $permissions = Invoke-RestMethod -Uri "$managementUrl/api/permissions/$vhost/$user" -Headers $headers -TimeoutSec 10
    if ($permissions.configure -ne '.*' -or $permissions.write -ne '.*' -or $permissions.read -ne '.*') {
        throw 'Development broker user lacks expected permissions on its vhost.'
    }
    Write-Output "PASS RabbitMQ $($overview.rabbitmq_version) management UI, authenticated API and vhost permissions"

    # A protocol greeting verifies the published port speaks AMQP, without adding a messaging client or topology.
    # Authentication is checked through management above; this does not publish or consume messages.
    $amqpClient = [Net.Sockets.TcpClient]::new()
    try {
        $null = $amqpClient.ConnectAsync('127.0.0.1', $amqpPort).WaitAsync([TimeSpan]::FromSeconds(5)).GetAwaiter().GetResult()
        $stream = $amqpClient.GetStream()
        $stream.ReadTimeout = 5000
        $stream.WriteTimeout = 5000
        $greeting = [byte[]](65, 77, 81, 80, 0, 0, 9, 1)
        $stream.Write($greeting, 0, $greeting.Length)
        $reply = [byte[]]::new(11)
        $stream.ReadExactly($reply, 0, $reply.Length)
        if ($reply[0] -ne 1 -or $reply[1] -ne 0 -or $reply[2] -ne 0 -or
            $reply[7] -ne 0 -or $reply[8] -ne 10 -or $reply[9] -ne 0 -or $reply[10] -ne 10) {
            throw 'RabbitMQ did not respond with AMQP Connection.Start.'
        }
        Write-Output "PASS RabbitMQ AMQP 0-9-1 handshake on published port $amqpPort"
    }
    finally { $amqpClient.Dispose() }

    Write-Output 'Infrastructure checks passed. No application tables, events, publishers or consumers were created.'
}
finally { Pop-Location }
