# Phase 03 — Catalog microservice

Status: COMPLETED. Date: 2026-09-22.

## What and why

Implemented product/category creation, full update, deletion, details and paginated/searchable lists.
This first business service owns catalog data and authoritative prices independently. Reusable Domain
validation and thin HTTP endpoints separate rules from transport. EF writes and Dapper projections
demonstrate a command/query split without a CQRS framework.

## How the request and data flow works

POST binds ProductRequest, calls CatalogService, validates Product invariants, checks the category via
ICatalogReader/Dapper and commits through ICatalogWriter/EfCatalogWriter. CatalogDbContext writes only
catalog_db. PostgreSQL enforces local references and category uniqueness under concurrency. HTTP returns
201, Location and DTO. GET uses parameterized Dapper SQL, local joins and projection DTOs.
Application errors become 400/404/409 ProblemDetails; unexpected errors become generic 500 responses.
Cancellation reaches both database paths; structured logs identify writes and trace IDs.

Concept: service contracts and database ownership are independent boundaries. Future Inventory references
product IDs without sharing Catalog entities, database access or migrations.

## Main files and decisions

- Domain: Product, Category, CatalogRules, CatalogValidationException; package-free.
- Application: CatalogService, requests/DTOs/search results, reader/writer boundaries, exceptions; package-free.
- Infrastructure: CatalogDbContext, EF writer, Dapper reader, typed options/DI, design-time factory,
  seed and migration. EF/Relational/Design 10.0.12, Npgsql/provider 10.0.3, Dapper 2.1.86.
  Design is private; dotnet-ef 10.0.12 is pinned in .config/dotnet-tools.json.
- API: endpoint groups, exception handler, Development OpenAPI/SwaggerUI 10.2.3 and explicit --seed mode.
- Catalog.Unit.Tests, Catalog.Integration.Tests and updated architecture project inventory.
- Test-Catalog.ps1 prepares private settings; Test-Skeleton.ps1 verifies current Catalog behavior.
- ADR-015 records normalized uniqueness, FK RESTRICT, USD price, last-write-wins PUT,
  explicit migration/seed and real PostgreSQL tests. No generic repository/cross-service references.

## Database and APIs

Migration `20260922174057_InitialCatalog` applied with catalog_app. Creates categories/products,
unique/category indexes, restricted FK, numeric price check and EF history. Other databases remain empty.
Seed ran twice: 2 categories/5 products. Stable IDs and full contract are in [catalog.md](../catalog.md).
Both `/api/catalog/products` and `/api/catalog/categories` expose GET list/details, POST, PUT and DELETE.
Development serves `/swagger` and `/openapi/v1.json`.

## Verified commands and results

From the repository root in PowerShell:

```powershell
./scripts/Test-Infrastructure.ps1
dotnet tool restore
./scripts/Set-ServiceEnvironment.ps1 -Service Catalog
dotnet ef migrations add InitialCatalog --project src/Services/Catalog/Catalog.Infrastructure --output-dir Migrations
dotnet ef migrations script --project src/Services/Catalog/Catalog.Infrastructure --output artifacts/catalog-migration.sql
dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure
dotnet run --project src/Services/Catalog/Catalog.Api --no-build --launch-profile http -- --seed
# Seed command repeated successfully.
dotnet ef migrations has-pending-model-changes --project src/Services/Catalog/Catalog.Infrastructure
dotnet build MicroShop.sln --no-restore
./scripts/Test-Catalog.ps1
./scripts/Test-Skeleton.ps1
```

Migration-add is the historical generation command; do not repeat it for normal startup.
Restore succeeded, full build had 0 warnings/errors; model check reported no pending changes.
All 30 tests passed without skips: 4 architecture, 9 unit/application, 17 PostgreSQL integration.
Coverage includes CRUD, missing resources, malformed/invalid input, price precision, mutation integrity,
concurrent duplicates, restricted deletion, literal search/injection strings, pagination/category filters,
seed preservation/idempotence and OpenAPI/Swagger. Tests also ran with public migration already present
to verify schema isolation. All seven HTTP hosts passed; test hosts stopped afterward.
Read-only inspection confirmed 2/5 seed rows, one migration, zero test schemas and zero tables in other
service databases. Dependency containers remain healthy. No volumes were reset.

## Problems, limits and remaining work

EF Relational patch conflict resolved with an explicit 10.0.12 reference; final build is clean.
Initial history SELECT logged a missing table, then initialization/migration succeeded.
No Phase 3 blocker remains. Security is Phase 9; usage is local learning. USD only, last-write-wins PUT,
possibly drifting count/page reads and sequential seed jobs are intentional limits. No optimistic tokens,
stock logic, service clients, Gateway routes or messaging yet.
Handoff, roadmap, architecture, ADR, changelog and README updated. Stop before Phase 4 Inventory.
