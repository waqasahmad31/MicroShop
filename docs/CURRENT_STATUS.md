# Current status

Last updated: 2026-09-24 (Asia/Karachi).
Phases 0–6: COMPLETED. Next: Phase 7 API Gateway, NOT STARTED.
Stop before Phase 7 until the user instructs continuation. Aspire/Azure and Kubernetes/AKS remain future.

## Implemented

- 23 source projects and seven test projects; production service/layer boundaries remain enforced.
- Restricted PostgreSQL owners and RabbitMQ development infrastructure, with persistent volumes.
- Catalog CRUD; Inventory stock records and safe adjustments; Ordering aggregates, snapshots and history.
- POST /api/orders validates IDs/quantities, merges duplicate products and obtains authoritative prices
  and availability through typed Catalog/Inventory HTTP clients before saving a Pending order.
- Configurable HTTP origins, finite per-call/checkout deadlines, body-size bounds and cancellation.
- Sanitized ProblemDetails for bad input, unavailable stock and failed/malformed/timed-out dependencies.
- EF atomic writes and Dapper reads; no cross-service SQL, no network-spanning transaction.
- Three services retain their existing migrations/seeds. Phase 6 adds no migration, stock effect or event.

Read [synchronous communication](synchronous-communication.md), [request flow](request-flow.md),
[Ordering](ordering.md), [Catalog](catalog.md), [Inventory](inventory.md) and ADR-018 in [decisions](DECISIONS.md).

## Phase 6 verification

| Command/check | Actual result |
|---|---|
| docker compose up -d --wait --wait-timeout 45 | Existing PostgreSQL/RabbitMQ healthy after Desktop recovery |
| ./scripts/Test-Infrastructure.ps1 | 4 owner logins, 4 wrong-password denials, 12 cross-service denials; HTTP/AMQP passed |
| dotnet build src/Services/Ordering/Ordering.Api --disable-build-servers -m:1 --nologo | 0 warnings/errors |
| ./scripts/Test-All.ps1 | 141 passed, 0 failed/skipped |
| dotnet build MicroShop.sln --no-restore --disable-build-servers -m:1 --nologo | 0 warnings/errors |
| ./scripts/Test-Skeleton.ps1 | Seven hosts passed; DB reads, Swagger and invalid checkout 400; hosts stopped |
| Public data inspection | Catalog 2 categories/5 products; Inventory 5 items/110 on-hand/0 reserved; Ordering 2 orders/3 lines |
| Ordering history inspection | Original Pending 1059.97 USD and Cancelled 79.99 USD preserved |
| Fixture cleanup / Identity | Zero test schemas in all three databases; Identity has no application tables |
| Publication checks | Six local passwords absent from publishable files; .env ignored/untracked; relative Markdown links and whitespace passed |

Breakdown: architecture 4; Catalog 9 unit + 17 integration; Inventory 14 + 27; Ordering 25 + 45.
The previous 108 tests remain, with the Phase 5 HTTP boundary assertion updated for authorized checkout;
33 new checks cover use-case validation and real HTTP checkout/dependency failures. No skipped checks.

Checkout integration hosts three real Kestrel APIs on dynamic loopback ports using separate restricted
roles and disposable schemas, plus fault servers. It verifies authoritative totals, immutable snapshots
across Catalog edits, unchanged stock, duplicate aggregation, no partial orders, missing products/stock,
invalid payloads, redirects, response limits, network failures, slow headers/body, overall deadlines and
caller cancellation. No browser automation is claimed. Details: [Phase 6](phases/phase-06-synchronous-communication.md).

## Environment and resolved issues

Docker Desktop initially could not start because stale Windows socket reparse files were inaccessible.
The affected socket-only directories under LOCALAPPDATA (Docker/run and docker-secrets-engine) were
renamed with .stale-phase6 timestamps, preserving them; Desktop regenerated runtime sockets and started.
No database/container volumes were reset, no Docker factory reset or secret-store contents were changed.
Dependency containers are healthy; application/test hosts were stopped after verification.

An initial Kestrel fixture used the shared default port. Explicit loopback Listen on port 0 fixed binding.
Derived factory clients also needed StartServer before client creation to select their own bound address.
A connection-refusal test needed a 5-second deadline to distinguish OS refusal from the separate timeout
case on Windows. Final full tests passed after these fixture corrections; production behavior was retained.
Build concurrency remains bounded because unrestricted MSBuild exhausted memory in Phase 5.

Repository: D:\WAQAS\Waqas_Projects\MicroShop.
Public remote: https://github.com/waqasahmad31/MicroShop; main / origin.
Maintain detailed phase-wise commits without attribution trailers; local .env remains ignored/untracked.
PostgreSQL 17.11 / RabbitMQ 4.2.9; local ports 5433, 5672, 15672. Native PostgreSQL owns 5432.
Volumes remain microshop_postgres_data and microshop_rabbitmq_data. Each service uses only its own *_db/*_app.

## Limits and stopping point

- Availability is a point-in-time check, not a reservation. Checkout never decrements stock or confirms orders.
- Price/stock reads are sequential and not a distributed snapshot; the overall deadline bounds the cart.
- Customer ID input/history filtering is not authorization. Identity/ownership enforcement starts in Phase 9.
- No creation idempotency or blind retries; cancellation or a lost response around commit is ambiguous.
- No public status/cancellation endpoint, messaging, Outbox/Inbox, gateway route, UI or cloud work in this phase.
- Catalog remains last-write-wins; Inventory deltas must not be retried after uncertain responses.
- Tests need all three service-specific connections, prepared/restored by Test-All. Interrupted runs can leave
  disposable schemas; inspect ownership before cleanup. Public seed data remains unchanged.

Stop before Phase 7 until instructed to continue.
