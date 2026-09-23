# Current status

Last updated: 2026-09-23 (Asia/Karachi).
Phases 0–4: COMPLETED. Next: Phase 5 Ordering, NOT STARTED; wait for the user's instruction.
Aspire/Azure Phases 19–28 and the separate Kubernetes/AKS track remain FUTURE / NOT STARTED.

## Implemented
- 23 source projects and five test projects; service/layer boundaries enforced.
- PostgreSQL/RabbitMQ infrastructure with restricted database owners.
- Catalog product/category CRUD, EF writes, Dapper reads, validation, migration and deterministic seed.
- Inventory item creation, stock deltas, details/pagination and on-hand/reserved/available invariants.
- Inventory adjustments lock the current row in a short transaction before Domain validation and EF save.
- Both APIs have ProblemDetails, trace IDs, structured command logs and Development Swagger/OpenAPI.
- Explicit migrations and Development seeds; normal HTTP startup modifies no schema or seed data.
- Topic guides: [Catalog](catalog.md), [Inventory](inventory.md), [ownership](database-ownership.md),
  [EF/Dapper](efcore-vs-dapper.md). ADR-016 records stock concurrency choices.

## Phase 4 verification

| Command/check | Actual result |
|---|---|
| ./scripts/Test-Infrastructure.ps1 | 4 owner logins, 4 wrong-password denials, 12 cross-service denials; broker HTTP/AMQP passed |
| dotnet tool restore | dotnet-ef 10.0.12 restored |
| EF migration generation / SQL review | 20260923090026_InitialInventory; only inventory_items and EF history |
| dotnet ef database update --project src/Services/Inventory/Inventory.Infrastructure | Applied with inventory_app to inventory_db |
| Inventory.Api --seed in Development, twice | Five items remain at 10/25/40/15/20 on-hand, zero reserved |
| dotnet ef migrations has-pending-model-changes --project src/Services/Inventory/Inventory.Infrastructure | No pending model changes |
| dotnet build MicroShop.sln --no-restore | 0 warnings, 0 errors |
| ./scripts/Test-All.ps1 | 71 passed, 0 failed/skipped; restore succeeded |
| ./scripts/Test-Skeleton.ps1 | All seven hosts passed, including Catalog/Inventory DB reads and Swagger; hosts stopped |
| Database inspection | Catalog 2 business tables + history; Inventory 1 business table + history; Identity/Ordering empty |
| Test cleanup / seed inspection | Zero Catalog/Inventory test schemas remain; 5 Inventory rows total 110 on-hand and 0 reserved |
| Publication checks | Six local passwords absent from non-ignored files; .env ignored/untracked; relative documentation links and diff whitespace passed |

Test breakdown: 4 architecture, 9 Catalog unit/application, 17 Catalog integration, 14 Inventory
unit/application and 27 Inventory integration. The suite also passed after the normal Inventory public
migration existed, checking isolation between public and disposable test schemas.
Two test hosts accepted all 32 concurrent increments without loss. Competing deductions could not exceed
available stock, including a nonzero reserved state. A held database lock test proved the waiting writer
used the newly committed quantity. Database constraints reject invalid direct SQL changes.
Historical verification remains in the Phase 1–3 records; exact Phase 4 commands are in
[its record](phases/phase-04-inventory.md).

## Environment and repository

Repository: D:\WAQAS\Waqas_Projects\MicroShop.
Public remote: https://github.com/waqasahmad31/MicroShop; branch main, remote origin.
Maintain detailed phase-wise commits; .env remains ignored/untracked.
PostgreSQL 17.11 and RabbitMQ 4.2.9 remain running/healthy. Published ports are localhost-only:
PostgreSQL 5433 (native PostgreSQL occupies 5432), AMQP 5672 and management 15672.
Volumes: microshop_postgres_data and microshop_rabbitmq_data; no Phase 4 reset.
Catalog still has 2 categories/5 products. Inventory has 5 matching documented external IDs.
Each service uses its own *_app role and *_db database; applications never use microshop_admin.
Application processes are stopped after smoke verification.

## Limits and stopping point

- Security is Phase 9; current APIs are unauthenticated localhost learning.
- Inventory has no product existence lookup, order reservation/release workflow, ledger or idempotency key.
  Never blindly retry a stock POST after a lost response. Availability reads are not reservations.
- Reserved invariants are present; only tests arrange nonzero reserved state until Phase 11.
- Catalog PUT remains last-write-wins; list count/pages can drift under concurrent edits; prices are USD only.
- Seeds run sequentially and preserve existing rows/stock. Tests require real PostgreSQL and separate
  CATALOG_TEST_CONNECTION_STRING / INVENTORY_TEST_CONNECTION_STRING, prepared by Test-All.ps1.
- Interrupted tests can leave disposable schemas; inspect ownership before cleanup.
- First-time EF migration history probing logs a missing-table SELECT, then successfully creates/applies it.
- No browser automation is claimed. Ordering, Gateway routes, UI, security, messaging and observability
  remain later phases. No cloud resources, Aspire projects or application Dockerfiles were introduced.

Stop before Phase 5 until instructed to continue.
