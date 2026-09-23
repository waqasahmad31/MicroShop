# Phase 05 — Ordering microservice

Status: COMPLETED. Date: 2026-09-23.

## What and why

Implemented Order/OrderItem/status with immutable product name/price snapshots, bounded quantities,
exact derived totals, duplicate aggregation and guarded local state transitions. Ordering persists
independently in ordering_db, so Catalog edits cannot rewrite order history.
The HTTP contract exposes details and customer history only: public checkout waits for authoritative
Catalog/Inventory validation in Phase 6. Narrow client interfaces exist with no HTTP implementations.

## How requests and data flow

Trusted in-process priced lines enter OrderingService, which constructs a Pending aggregate using a
generated ID and TimeProvider UTC timestamp. Domain validates/copies/merges items. One EF SaveChanges
transaction persists header and lines, rolling everything back if any row fails.
Status commands lock the current order row, refresh even pretracked state, apply Domain rules and
commit. This prevents incompatible concurrent outcomes from overwriting terminal state.
GETs call parameterized Dapper projections over local snapshots and calculate totals. No cross-service
SQL, foreign keys, HTTP calls or domain/project references are used.

Concepts: aggregate atomicity, historical snapshots, local state machine, read projections and explicit
staging of trusted checkout. Confirmation/cancellation currently represent local state only, not stock effects.

## Main implementation

- Ordering.Domain: Order, OrderItem, OrderStatus and validation/transition exceptions; no packages.
- Ordering.Application: OrderingService, priced-line and read DTOs, IOrderingReader/IOrderingWriter,
  ICatalogServiceClient/IInventoryServiceClient contracts; no packages.
- Ordering.Infrastructure: DbContext/configuration, atomic EF writer, Dapper reader, typed options/DI,
  design-time factory, deterministic seed and migration. Reuses EF/Relational/Design 10.0.12,
  Npgsql/provider 10.0.3, Dapper 2.1.86; existing dotnet-ef tool.
- Ordering.Api: GET routes, ProblemDetails, Development SwaggerUI 10.2.3 and explicit --seed command.
- Ordering.Unit.Tests / Ordering.Integration.Tests; architecture inventory extended for seven test projects.
- Test-All supplies ORDERING_TEST_CONNECTION_STRING separately; smoke script verifies Ordering GET/POST 405.
- ADR-017 and [Ordering guide](../ordering.md) document exact invariants, routes, seed IDs and transitions.

## Schema, seed and contract

Migration 20260923111937_InitialOrdering creates orders, order_items, EF history and customer/date/ID index.
Local FK order_items.order_id -> orders.id; customer/product IDs are external. Checks enforce row IDs,
status/reason combinations, quantity/name/price bounds. Aggregate limits/transitions remain Domain rules.
Totals are derived, not separate stored columns.
Seed creates 2 synthetic orders and 3 item rows: Pending Laptop/Mouse (1059.97 USD), Cancelled Keyboard
(79.99 USD). Repeated seed preserves existing orders; tests also verify preserved status changes.
GET /api/orders/{id} returns detail; GET /api/orders?customerId={id} returns paginated history.
No HTTP create/cancel/status endpoints. POST on the collection returns 405. OpenAPI exposes GETs only.
Current customer filtering is not authorization; Phase 9 must add authenticated ownership checks.

## Exact verification

From repository root in PowerShell:

```powershell
./scripts/Test-Infrastructure.ps1
dotnet build src/Services/Ordering/Ordering.Api/Ordering.Api.csproj
dotnet tool restore
./scripts/Set-ServiceEnvironment.ps1 -Service Ordering
dotnet ef migrations add InitialOrdering --project src/Services/Ordering/Ordering.Infrastructure --output-dir Migrations
./scripts/Test-All.ps1
dotnet ef migrations script --no-build --project src/Services/Ordering/Ordering.Infrastructure --output artifacts/ordering-migration.sql
dotnet ef database update --no-build --project src/Services/Ordering/Ordering.Infrastructure
dotnet run --project src/Services/Ordering/Ordering.Api --no-build --launch-profile http -- --seed
# Seed command repeated successfully.
dotnet ef migrations has-pending-model-changes --no-build --project src/Services/Ordering/Ordering.Infrastructure
dotnet build MicroShop.sln --no-restore --disable-build-servers -m:1
./scripts/Test-Skeleton.ps1
$env:ORDERING_TEST_CONNECTION_STRING = $env:ConnectionStrings__Database
dotnet test tests/Ordering.Integration.Tests/Ordering.Integration.Tests.csproj --no-build --no-restore --disable-build-servers -m:1
```

Migration-add is the historical generation command, not routine startup.
Ordering API and final full solution builds passed with zero warnings/errors; model matches migration.
108 tests passed with zero failures/skips: architecture 4, Catalog 9/17, Inventory 14/27, Ordering 21/16
(unit/application / real PostgreSQL integration). Ordering's 16 integration checks also passed after the
normal public migration existed, verifying independent disposable-schema migration history.

Tests cover exact/max totals, snapshot isolation, duplicate aggregation/limits, state rules/replays,
atomic rollback on an invalid item, concurrent incompatible outcomes, stale tracked-state refresh,
customer isolation/order/pagination, invalid requests, seed preservation and blocked HTTP writes.
All seven actual HTTP hosts passed, including Ordering database history/Swagger and POST 405.
Application hosts stopped afterward; healthy dependency containers remain. No volumes were reset.
Final inspection found zero test schemas across all three service databases and unchanged Catalog/Inventory
seed data. Docker Desktop had stopped after testing; it was restarted and persisted order totals/statuses
and dependency health were reverified. Local credentials and relative documentation links were checked.

## Issues and limits

The first default-parallel solution test invocation failed in MSBuild with OutOfMemoryException.
About 455 MB physical memory was free on the 16 GB machine. Test-All now disables persistent build
servers and uses -m:1. The bounded rerun passed every test; final build is clean. No feature/test was removed.
Initial migration history probing logged a missing-table SELECT before successful initialization.

No remaining Phase 5 blocker. Public checkout, authoritative HTTP lookups, timeouts and dependency failures
are Phase 6. Reservation/release coordination is Phase 11; no current stock effects, messaging or Outbox/Inbox.
No creation idempotency, authentication or production readiness is claimed. UTC/USD/bounded lines and
sequential seed commands are explicit choices. No browser automation.
Persistent handoff, architecture, roadmap, guides, changelog and ADR updated; stop before Phase 6.
