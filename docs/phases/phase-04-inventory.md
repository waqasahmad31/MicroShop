# Phase 04 — Inventory microservice

Status: COMPLETED. Date: 2026-09-23.

## What and why

Implemented InventoryItem creation, stock additions/deductions, details and paginated reads.
Inventory owns OnHand/Reserved and derives Available. It refers to ProductId without accessing Catalog's
database or assemblies. This teaches independent ownership and why stock deltas need concurrency control
beyond Catalog's last-write-wins descriptive edits.

## How requests and data flow

The endpoint binds input; InventoryService validates required/nonzero values and calls an atomic writer.
EfInventoryWriter starts a Read Committed transaction, loads the current row with parameterized
SELECT FOR UPDATE, calls InventoryItem.AdjustOnHand, saves through EF and commits before returning a DTO.
The rule rejects below-reserved stock/overflow without mutating the entity. A concurrent writer waits,
then validates the latest committed quantities. Failure disposes/rolls back the transaction.
Dapper projects GET responses, deriving Available in SQL; each request uses only inventory_db.
Domain/Application remain framework-free. No distributed transaction or in-process locking is used.

## Implementation inventory

- Inventory.Domain: InventoryItem, validation and stock-conflict exceptions.
- Inventory.Application: InventoryService, request/read models, IInventoryReader and atomic IInventoryWriter.
- Inventory.Infrastructure: DbContext, EF writer, Dapper reader, typed options/DI, design-time factory,
  deterministic seed and migration. Same verified versions as Catalog: EF/Relational/Design 10.0.12,
  Npgsql/provider 10.0.3, Dapper 2.1.86. Existing local EF tool reused.
- Inventory.Api: thin endpoints, ProblemDetails handler, Development SwaggerUI 10.2.3 and explicit --seed mode.
- Inventory.Unit.Tests / Inventory.Integration.Tests and extended architecture inventory; no new service refs.
- Test-All.ps1 supplies separate private Catalog/Inventory test connections and restores environment settings.
  Test-Catalog.ps1 remains compatible; Test-Skeleton.ps1 adds Inventory configuration/database/Swagger checks.
- ADR-016, Inventory guide, database/EF guides, architecture, roadmap, README and mandatory handoff updated.

## Migration, seed and API

Migration 20260923090026_InitialInventory creates inventory_items and EF history in inventory_db.
ProductId is the PK; checks enforce nonempty ID, OnHand >= 0 and 0 <= Reserved <= OnHand.
Available is derived, not stored; there is no Catalog FK.
Five documented Catalog seed IDs get stock 10/25/40/15/20 with zero reserved. Seed ran twice without
overwriting existing records. Integration tests additionally verified that adjusted stock survives seed reruns.
Catalog remains at 2 categories/5 products. Identity/Ordering have no application tables.

Routes: POST /api/inventory/items (201 + Location), GET list/details (200),
POST /api/inventory/items/{productId}/adjustments (200). Invalid input 400, missing item 404,
duplicate/stock conflicts 409, generic unexpected error 500. Development OpenAPI/Swagger are enabled.
The [Inventory guide](../inventory.md) documents full bodies, invariants, limits and learning flow.

## Exact verification commands

From the repository root in PowerShell:

```powershell
./scripts/Test-Infrastructure.ps1
dotnet build src/Services/Inventory/Inventory.Api/Inventory.Api.csproj
dotnet tool restore
./scripts/Set-ServiceEnvironment.ps1 -Service Inventory
dotnet ef migrations add InitialInventory --project src/Services/Inventory/Inventory.Infrastructure --output-dir Migrations
dotnet ef migrations script --project src/Services/Inventory/Inventory.Infrastructure --output artifacts/inventory-migration.sql
dotnet ef database update --project src/Services/Inventory/Inventory.Infrastructure
dotnet run --project src/Services/Inventory/Inventory.Api --no-build --launch-profile http -- --seed
# The seed command was repeated successfully.
dotnet ef migrations has-pending-model-changes --project src/Services/Inventory/Inventory.Infrastructure
dotnet build MicroShop.sln --no-restore
./scripts/Test-All.ps1
./scripts/Test-Skeleton.ps1
```

Migration-add is the historical generation command, not a normal startup step.
Both builds passed with zero warnings/errors; restore succeeded; model matches migration.
71 tests passed with no failures/skips: 4 architecture, 9/17 Catalog unit/integration, 14/27 Inventory
unit/integration. All previous tests remain passing. Tests use isolated real PostgreSQL schemas, not an
in-memory provider, and passed again after the public Inventory migration existed.

Concurrency evidence: 32 increments across two hosts all committed with unique resulting quantities 1–32;
competing deductions accepted only available units for both zero/nonzero reserved states; a database
blocker was observed before committing a competing change, then the waiting adjustment correctly rejected
insufficient stock. Raw SQL invariant violations failed; rejected adjustments preserved state and released locks.
Tests also cover invalid/malformed input, integer overflow, missing items, pagination, duplicate creates,
seed repeatability and OpenAPI/Swagger. Seven real HTTP hosts passed, including both database-backed APIs.
Application hosts stopped after smoke checks; healthy dependency containers remain running. No volume resets.

## Issues, limits and remaining work

No Phase 4 blocker remains. Initial EF history probing logged an expected missing-table SELECT before
successful initialization. Reserved is represented/enforced, but order reservations/releases are Phase 11.
Product existence validation, HTTP clients, checkout and order workflows remain later lessons.
There is no stock audit ledger or idempotency key; uncertain adjustment outcomes must not be blindly retried.
Pagination count/page is not a frozen snapshot. Sequential seed commands preserve existing quantities.
Security remains Phase 9; current APIs are local exercises. No browser automation or production readiness claimed.
Stop before Phase 5 Ordering; wait for the user's next instruction.
