# Current status

Last updated: 2026-09-23 (Asia/Karachi).
Phases 0–5: COMPLETED. Next: Phase 6 synchronous service communication, NOT STARTED.
Wait for the user's continuation instruction. Aspire/Azure Phases 19–28 and Kubernetes/AKS remain future.

## Implemented
- 23 source projects and seven test projects, with enforced layer/service boundaries.
- Restricted PostgreSQL database owners and RabbitMQ development infrastructure.
- Catalog product/category CRUD; Inventory stock items and safe concurrent adjustments.
- Ordering aggregate snapshots, exact derived totals, duplicate-line aggregation and local state transitions.
- Atomic EF order writes and locked status changes; Dapper details/paginated customer history.
- Ordering HTTP exposes GETs only. No public checkout/status mutation or service HTTP implementation yet.
- Three services have explicit migrations, repeatable Development seeds, ProblemDetails and Swagger/OpenAPI.
- Read [Ordering](ordering.md), [Catalog](catalog.md), [Inventory](inventory.md), [ownership](database-ownership.md)
  and [EF/Dapper](efcore-vs-dapper.md). ADR-017 records the Phase 5 boundary and decisions.

## Phase 5 verification

| Command/check | Actual result |
|---|---|
| ./scripts/Test-Infrastructure.ps1 | 4 owner logins, 4 wrong-password denials, 12 cross-service denials; broker HTTP/AMQP passed |
| Ordering API build | 0 warnings/errors |
| dotnet tool restore | dotnet-ef 10.0.12 restored |
| EF generation / SQL review | 20260923111937_InitialOrdering; orders/order_items/history only |
| EF database update using ordering_app | Applied to ordering_db |
| Ordering.Api --seed twice | 2 orders / 3 item rows preserved; totals 1059.97 and 79.99 USD |
| EF pending-model check | No changes since migration |
| dotnet build MicroShop.sln --no-restore --disable-build-servers -m:1 | 0 warnings, 0 errors |
| ./scripts/Test-All.ps1 | 108 passed, 0 failed/skipped, including all previous 71 checks |
| Ordering integration after public migration | 16 passed; disposable-schema migration history remained independent |
| ./scripts/Test-Skeleton.ps1 | Seven hosts passed; DB reads/Swagger and Ordering POST 405 verified; hosts stopped |
| Final data/schema inspection | Ordering 2 orders/3 lines; Catalog 2 categories/5 products; Inventory 5 items/110 on-hand/0 reserved; no test schemas remain |
| Publication checks | Six local passwords absent from publishable files; .env ignored/untracked; relative documentation links and whitespace checks passed |

Test breakdown: 4 architecture; Catalog 9 unit + 17 integration; Inventory 14 + 27; Ordering 21 + 16.
Ordering checks cover exact/max totals, snapshot copying/round trip, aggregate rollback, transitions,
concurrent incompatible outcomes, stale tracked state, customer filtering/pagination and disabled HTTP writes.
All integration tests use real PostgreSQL schemas. Exact commands are in [Phase 5](phases/phase-05-ordering.md).

## Environment and issues

The first unrestricted parallel solution test attempt failed during MSBuild with OutOfMemoryException;
no passing feature run is claimed for that attempt. Free physical memory was about 455 MB on a 16 GB machine.
Test-All now uses --disable-build-servers -m:1. The bounded full test run and build both passed.
No application feature or test was removed to address this environment issue.
As in earlier phases, first-time EF history probing logged a missing-table SELECT before successful creation.
Docker Desktop stopped after testing. It was started again; both containers became healthy and all
persisted seed counts/statuses/totals were rechecked successfully without resetting volumes.

Repository: D:\WAQAS\Waqas_Projects\MicroShop.
Public remote: https://github.com/waqasahmad31/MicroShop; main / origin.
Maintain detailed phase-wise commits; local .env is ignored/untracked.
PostgreSQL 17.11 and RabbitMQ 4.2.9 remain running; localhost ports are 5433, 5672 and 15672.
Native PostgreSQL uses 5432. Volumes remain microshop_postgres_data / microshop_rabbitmq_data; no reset.
Catalog seed: 2 categories/5 products. Inventory: 5 items, 110 total on-hand, zero reserved.
Ordering: 2 synthetic orders/3 snapshots. Identity still has no application tables.
Application processes are stopped after verification. Each service uses only its own *_db / *_app.

## Limits and stopping point

- Ordering's priced-line creation and status commands are in-process foundations only, not HTTP actions.
  Phase 6 must obtain authoritative prices and check availability before exposing checkout.
- Current order state transitions do not reserve/release Inventory stock. This is Phase 11 workflow work.
- Customer history filtering is not authorization; authenticated identity/ownership is Phase 9.
- No order creation idempotency, reservation messaging, Outbox/Inbox, audit ledger or production readiness.
- Catalog is USD-only/last-write-wins; Inventory deltas must not be blindly retried after uncertain responses.
- History count/page can drift during concurrent creation. Sequential seeds preserve existing rows.
- Tests need local PostgreSQL and three service-specific test connections; Test-All prepares/restores them.
  Interrupted processes can leave disposable schemas; inspect ownership before cleanup.
- No browser automation claimed. Gateway routes/UI/security/observability/cloud remain future phases.

Stop before Phase 6 until the user instructs continuation.
