# Current status

Last updated: 2026-09-22 (Asia/Karachi).
Phases 0–3: COMPLETED. Next: Phase 4 Inventory, NOT STARTED; wait for the user's instruction.
Aspire/Azure Phases 19–28 and the separate Kubernetes/AKS track remain FUTURE / NOT STARTED.

## Implemented
- 23 source projects and three test projects with enforced service/layer boundaries.
- PostgreSQL/RabbitMQ infrastructure and restricted database owners.
- Catalog product/category CRUD, Domain invariants and CatalogService use cases.
- EF writes/migration, Dapper details/search/pagination; category uniqueness and restricted deletion.
- ProblemDetails, trace IDs, structured command logs, Development OpenAPI/Swagger UI.
- Explicit Development seed: 2 categories/5 products with deterministic IDs, preserving edits.
- [Catalog guide](catalog.md), [database ownership](database-ownership.md), [EF/Dapper flow](efcore-vs-dapper.md), ADR-015.

## Phase 3 verification

| Command/check | Actual result |
|---|---|
| ./scripts/Test-Infrastructure.ps1 | 4 owner logins, 4 wrong-password denials, 12 cross-service denials; broker HTTP/AMQP checks passed |
| dotnet tool restore | Local dotnet-ef 10.0.12 restored |
| EF migration generation and SQL review | 20260922174057_InitialCatalog; Catalog tables only |
| dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure | Applied using catalog_app to catalog_db |
| Catalog.Api --seed in Development | Passed twice; 2 categories, 5 products |
| dotnet ef migrations has-pending-model-changes --project src/Services/Catalog/Catalog.Infrastructure | No changes since migration |
| dotnet build MicroShop.sln --no-restore | 0 warnings, 0 errors |
| ./scripts/Test-Catalog.ps1 | Restore succeeded; 30 passed, 0 failed/skipped: 4 architecture, 9 unit/application, 17 PostgreSQL integration |
| ./scripts/Test-Skeleton.ps1 | Seven hosts passed, including Catalog DB read/Swagger; hosts stopped afterward |
| Database inspection | Catalog has 2 business tables + EF history; Identity/Inventory/Ordering have zero application tables |
| Test schema inspection | No catalog_test_* schemas remain after completed runs |
| Publication checks | Six local passwords absent from non-ignored files; .env ignored/untracked; git diff --check passed |

Exact commands are in [the phase record](phases/phase-03-catalog.md).
EF commands require the Catalog service environment in the same terminal.
The empty-database history probe logged a missing-table SELECT before the successful initial migration.
A package conflict was resolved by explicitly pinning EF Relational 10.0.12; the final build is clean.

## Current environment

Repository: D:\WAQAS\Waqas_Projects\MicroShop.
Public remote: https://github.com/waqasahmad31/MicroShop; branch main, remote origin.
Phase-wise commits describe scope, decisions and verification. Local .env remains ignored.
PostgreSQL 17.11 and RabbitMQ 4.2.9 containers remain running and healthy.
Ports: PostgreSQL 127.0.0.1:5433 (native PostgreSQL occupies 5432), AMQP 5672, management 15672.
Volumes: microshop_postgres_data and microshop_rabbitmq_data; no reset in Phase 3.
Database/login pairs: identity_db/identity_app, catalog_db/catalog_app,
inventory_db/inventory_app, ordering_db/ordering_app. Applications never use microshop_admin.
Catalog HTTP host is stopped after verification; use the guide's startup commands when needed.

## Limits and stopping point

- Catalog is unauthenticated localhost learning; security is Phase 9.
- PUT is last-write-wins. List count/page may drift under concurrent edits. Currency is USD only.
- Seed runs sequentially; it preserves rows and does not resolve conflicting category names.
- Tests require local PostgreSQL; interrupted runs can leave disposable schemas to inspect/clean.
- No browser automation is claimed; Swagger HTML/OpenAPI and HTTP/database behavior were verified.
- Gateway routes, Inventory/Ordering business code, UI, Identity, messaging, Outbox/Inbox and observability
  remain later phases. RabbitMQ has no business publishers/consumers yet.
- No cloud resources, Aspire projects, application Dockerfiles or cross-service references were introduced.

Next action is Phase 4 only after user authorization. Historical evidence remains in Phase 1/2 records.
