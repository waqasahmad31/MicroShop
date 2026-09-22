# Current status

Last updated: 2026-09-22 (Asia/Karachi)
Current phase: Phase 2 COMPLETED. Phases 0–1 COMPLETED. Phase 3 NOT STARTED.
Authorization: Phase 2 infrastructure only. Stop before Phase 3.

## Completed
- The existing .NET 10 skeleton remains unchanged: 23 source projects, one xUnit architecture project,
  four layered APIs, Notification, Gateway and Blazor Web/Client; no business functionality.
- Infrastructure-only docker-compose.yml: PostgreSQL 17.11 and RabbitMQ 4.2.9 with management.
- Four separate databases/owner logins; PUBLIC CONNECT/TEMPORARY revoked, restricted roles and owned schemas.
- Named volumes, localhost port bindings, health checks, stable broker hostname, unless-stopped restart policy.
- .env.example plus ignored .env generated with distinct random development passwords.
- Helpers for environment creation, per-service connection settings and real infrastructure checks.
- Docker runbook, Phase 2 record, ADR-013 and persistent handoff updated.

## Current running infrastructure
| Service | Container | State at final verification | Host ports | Volume |
|---|---|---|---|---|
| postgres | microshop-postgres-1 | healthy | 127.0.0.1:5433 -> 5432 | microshop_postgres_data |
| rabbitmq | microshop-rabbitmq-1 | healthy | 127.0.0.1:5672, 127.0.0.1:15672 | microshop_rabbitmq_data |

Default Compose network: microshop_default. Both containers are intentionally left running.
Management UI: http://localhost:15672. Credentials are in ignored .env, not documentation.
A native PostgreSQL process uses host 5432. Repository default/example stays 5432; local .env overrides 5433.
Use docker compose down from this repository to stop without deleting data.

Database/login pairs: identity_db/identity_app, catalog_db/catalog_app,
inventory_db/inventory_app, ordering_db/ordering_app. Administrator: microshop_admin.
All four service databases have ZERO application tables and no EF migrations.
No publishers/consumers, custom exchanges/queues or message events exist.
Temporary persistence-check metadata was removed by the verified clean reset.

## Last verified commands and results
| Command/check | Result |
|---|---|
| docker version; docker compose version | Linux engine 29.1.3, Compose v2.40.3 available |
| docker compose config --quiet | Passed with local .env |
| docker compose --env-file .env.example config --quiet | Passed with public development examples |
| docker compose up -d; docker compose ps | Both services healthy |
| ./scripts/Test-Infrastructure.ps1 | Passed initially, after recreation and after clean reset |
| docker compose exec -T postgres bash /microshop-verify/verify-isolation.sh | 4 owner logins, 4 wrong-password denials, all 12 cross-service denials passed |
| RabbitMQ management HTML / API / permissions | HTTP 200, authenticated access and vhost grants passed |
| Published AMQP protocol greeting | Connection.Start response received on 5672; no publish/consume |
| docker compose down; docker compose up -d --wait --wait-timeout 120 | New container IDs; PostgreSQL comment and RabbitMQ test vhost persisted |
| docker compose down -v; docker compose up -d --wait --wait-timeout 120 | Scoped clean initialization passed; old markers absent, all roles/DBs recreated |
| All 4 service DB table counts | Zero application tables |
| ./scripts/Set-ServiceEnvironment.ps1 -Service <each service> | Correct database/login/local port/password presence; value not logged |
| dotnet restore MicroShop.sln | Exit 0; all projects up-to-date |
| dotnet build MicroShop.sln --no-restore | Exit 0; 0 warnings, 0 errors; 11.00 seconds |
| dotnet test MicroShop.sln --no-build --no-restore | Exit 0; 4 passed, 0 failed, 0 skipped |
| git check-ignore .env / tracked-file check | Ignored and not tracked |
| Local credential scan / environment generator overwrite check | Six distinct passwords absent from non-ignored files; existing .env preserved |
| git diff --name-only -- src tests | Empty; no source/project/package/test changes |

Before deleting volumes, verified exact names and Compose ownership labels. They were new Phase 2
volumes containing only bootstrap state and test metadata. Unrelated Docker volume inventory remained unchanged.
No phase is marked complete based only on configuration inspection.

## Not started
Catalog/Product/Category or other business entities; DbContexts, EF/Npgsql/Dapper packages/migrations;
API clients; Gateway business routes; MudBlazor UI; application Identity/JWT/refresh;
RabbitMQ.Client, event bus/consumers/publishers, Outbox/Inbox; OpenTelemetry;
application Dockerfiles; Azure; .NET Aspire.

## Known issues and limits
- No Phase 2 blocker. Docker is now available; the Phase 1 offline-engine observation is historical.
- Host port 5432 conflict is handled locally using 5433; do not stop the existing native PostgreSQL.
- PostgreSQL health is server readiness, not proof of permissions. Run the infrastructure verifier for grants/auth.
- Expected negative-authentication/CONNECT-denial tests produce PostgreSQL FATAL log entries.
  The AMQP greeting probe produces an expected closed-connection warning. These are verification effects.
- No browser login automation or business messaging test is claimed; management HTTP/API and AMQP greeting were tested.
- .env values bootstrap empty volumes only; editing passwords does not rotate persisted accounts.
- Phase 1 dotnet --info workload-metadata issue was not re-investigated; actual .NET commands passed.
- Earlier skeleton HTTP/auth limitations remain; application security is Phase 9.

## Repository state
Root: D:\WAQAS\Waqas_Projects\MicroShop. No commits or remote.
The 94 Phase 1 files were already staged when this phase began. Their staged snapshot was preserved;
Phase 2 changes/new files are unstaged. No commits created.
No other parent-workspace project was modified. No active work remains within Phase 2.
Next recommendation: Phase 3 Catalog, only after the user instructs continuation.
