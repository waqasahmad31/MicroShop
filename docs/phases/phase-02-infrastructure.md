# Phase 02 — Development infrastructure

Status: COMPLETED
Date: 2026-09-22
Scope: PostgreSQL and RabbitMQ only. Phase 3 NOT STARTED.

## What / why / concept
Implemented independent local databases/credentials and a running broker using infrastructure-only Compose.
The purpose is to make service data ownership and dependency lifecycle concrete before persistence code.
The learning concepts are database-per-service permissions, password authentication, bootstrap versus
migrations, and container lifecycle versus persistent storage.

## Architecture and flow
Compose reads ignored .env -> official images start on the default microshop_default network ->
empty PostgreSQL volume runs the version-controlled initializer -> four database owners/databases/grants ->
TCP becomes available. RabbitMQ creates its development user/vhost and enables management.
Hosts later read ConnectionStrings__Database from their own terminal environment; the current service
skeletons do not yet open database connections.

PostgreSQL 17.11-bookworm, RabbitMQ 4.2.9-management; localhost-only ports.
Default 5432 is occupied by native PostgreSQL on this machine, so local .env uses 5433.
RabbitMQ uses host 5672/15672 and stable node hostname microshop-rabbitmq.
Named volumes: microshop_postgres_data and microshop_rabbitmq_data.
No application Dockerfiles or network customization.

## Files created
- docker-compose.yml: two services, named volumes, health/restart/ports/environment.
- .env.example: documented public development examples.
- infra/postgres/init/01-service-databases.sh: roles/databases/ownership/grants, no tables.
- infra/postgres/verify/verify-isolation.sh: password/owner/privilege and cross-database checks.
- scripts/New-DevelopmentEnvironment.ps1: non-overwriting random local credential generation.
- scripts/Set-ServiceEnvironment.ps1: selected service connection-string environment preparation.
- scripts/Test-Infrastructure.ps1: real Docker/SQL/HTTP/AMQP checks.
- docs/docker.md: topology, configuration, commands, lifecycle/reset, known limitations and sources.
- Local ignored .env: generated credentials, POSTGRES_PORT=5433; not staged/tracked.

## Files modified
.gitattributes (LF shell/YAML/PowerShell), AGENTS.md, README.md, docs/PROJECT_CONTEXT.md,
docs/ARCHITECTURE.md, docs/IMPLEMENTATION_PLAN.md, docs/CURRENT_STATUS.md,
docs/NEXT_STEPS.md, docs/DECISIONS.md, docs/CHANGELOG.md and this phase record.
No application or .NET project/package/test files changed.

## Database changes / permission model
| Database | Owner/login | Own access | Other three service databases |
|---|---|---|---|
| identity_db | identity_app | Authenticated, owns DB/schema | Denied |
| catalog_db | catalog_app | Authenticated, owns DB/schema | Denied |
| inventory_db | inventory_app | Authenticated, owns DB/schema | Denied |
| ordering_db | ordering_app | Authenticated, owns DB/schema | Denied |

Service roles are not superusers and cannot create roles/databases, replicate or bypass RLS.
No memberships. PUBLIC database privileges revoked; matching owners granted database/schema access.
postgres/template1 are restricted too. microshop_admin remains the explicit bootstrap superuser.
All four service databases verified to contain zero application tables.
No DbContexts, migrations, repositories, cross-service foreign keys or application APIs added.

## Tests and actual verification
- Docker Linux engine available: 29.1.3; Compose v2.40.3.
- docker compose config --quiet: passed. Public .env.example also validates.
- docker compose up -d and docker compose ps: both containers healthy.
- ./scripts/Test-Infrastructure.ps1: passed before/after persistence test and after clean reset.
- PostgreSQL: four own-database TCP password logins; actual ownership/schema privileges and role restrictions;
  four wrong-password failures; all 12 directed cross-service connections denied with CONNECT errors.
- Published PostgreSQL TCP port 5433 reachable.
- RabbitMQ Management HTML returned HTTP 200; API authentication and microshop vhost permissions passed.
- Actual AMQP 0-9-1 greeting on 5672 returned Connection.Start. No messaging application code/topology.
- All four Set-ServiceEnvironment mappings verified without exposing credentials.
- git check-ignore .env passed; .env is not tracked.
- All six generated passwords are distinct and absent from non-ignored repository files.
  The environment generator refused to overwrite the existing .env, whose content hash stayed unchanged.
- dotnet restore MicroShop.sln: exit 0.
- dotnet build MicroShop.sln --no-restore: exit 0, 0 warnings/errors.
- dotnet test MicroShop.sln --no-build --no-restore: 4 passed, 0 failed/skipped.
- src/tests diff against the existing staged skeleton is empty.

## Persistence and clean initialization
Initial inspection showed neither MicroShop volume existed.
Created a catalog_db comment and a temporary RabbitMQ vhost as test markers; no tables/messages.
Ran docker compose down, then docker compose up -d --wait --wait-timeout 120.
Both markers survived and both container IDs changed, proving named-volume persistence through recreation.
Reran authentication/isolation/broker checks successfully.

Verified the exact new MicroShop volume names and project labels, then ran docker compose down -v
and docker compose up -d --wait --wait-timeout 120.
Both old volumes were removed and new ones initialized. Both markers were absent, service databases/logins
and initial broker vhost were recreated, and all checks passed again.
Compared unrelated volume inventory before/after: unchanged.
The two containers are left running and healthy with empty application databases.

## Problems, warnings and decisions
- Native PostgreSQL owns host 5432: resolved with local .env port 5433, keeping documented defaults.
- The Phase 1 Docker-offline condition no longer applies.
- The AMQP probe intentionally closes after greeting, producing a broker warning; SQL negative tests
  intentionally produce authentication/CONNECT-denial log entries.
- Health status is not proof of database authorization; explicit SQL tests provide that evidence.
- An initial ad hoc PowerShell mapping assertion used dictionary-style assignment on
  DbConnectionStringBuilder; corrected the assertion to its explicit property methods.
  The helper's generated connection strings were verified for all four services.
- ADR-013 records the concrete development topology, isolation/bootstrap model and port override.
- .env password edits do not rotate persisted accounts; bootstrap is only for empty volumes.

## Remaining work
None for Phase 2. Read docs/NEXT_STEPS.md for Phase 3 Catalog after an explicit instruction.
No Catalog entities, persistence packages, business APIs, event bus, Azure or Aspire were implemented.
