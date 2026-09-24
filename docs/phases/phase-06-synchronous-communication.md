# Phase 06 — Synchronous service communication

Status: COMPLETED
Completed: 2026-09-24 (Asia/Karachi).

## What and why

Ordering now accepts POST /api/orders with customerId and productId/quantity lines, reads authoritative
Catalog prices and Inventory availability through typed IHttpClientFactory clients, then saves one Pending
aggregate. It returns 201 + Location + details. This teaches real HTTP boundaries and dependency failures
while preserving separate databases and the existing EF-write/Dapper-read split. A client cannot set price,
name, total or status; unknown JSON fields are rejected. Customer identity remains local/unauthenticated.

## How the request and data flow

1. Ordering API binds the request and creates a linked caller/15-second checkout cancellation deadline.
2. CheckoutService validates all inputs: nonempty IDs, 1–100 non-null lines, 1–1000 units per product.
   Duplicate IDs merge before any network reads; combined quantities must still fit the limit.
3. CatalogServiceClient calls Catalog's product GET and validates identity, required name/price and USD.
4. InventoryServiceClient calls Inventory's item GET and validates identity, required quantities and arithmetic.
   Missing products/stock or insufficient combined stock returns 409 without creating any part of the order.
5. After all reads succeed, OrderingService constructs snapshots and EfOrderingWriter saves header/items
   in one local transaction. No transaction spans HTTP. No Inventory writes or stock reservation occurs.
6. Detail/history GETs use Dapper over stored snapshots; later Catalog edits cannot change existing orders.

See [request-flow](../request-flow.md) for Mermaid and [synchronous communication](../synchronous-communication.md)
for executable setup/request examples and configuration. No Gateway route, UI or RabbitMQ code was added.

## Projects, classes and API changes

- Ordering.Application: CheckoutRequest/CheckoutItem, CheckoutService and typed dependency failure kinds.
  Existing client interfaces are now implemented; Domain/Application remain package-free.
- Ordering.Infrastructure: CatalogServiceClient, InventoryServiceClient, small local wire DTOs/ServiceJson,
  ServiceCommunicationOptions and HttpClientFactory registrations. Microsoft.Extensions.Http 10.0.12 added.
- Ordering.Api: POST endpoint/deadline, strict unknown-field handling, sanitized ProblemDetails 409/502/503/504.
- Existing GET routes, atomic writer, aggregate transitions and migrations remain unchanged. Public status
  mutations still do not exist. Internal priced-line creation is never bound directly to an HTTP request.
- Tests: four new application checks; 29 new integration cases; prior price-submission boundary test now
  expects 400 rather than 405, still rejects public status changes and verifies actual typed registrations.
- Test harness references Catalog.Api and Inventory.Api entry points with aliases to host independent APIs.
  Architecture tests explicitly allow these test-only edges; no production cross-service references exist.
- Test-Skeleton now expects empty checkout JSON to return 400. No new project or infrastructure service.

## HTTP behavior and bounded work

Default origins are localhost:5220/5230; configurable Services options validate origins and finite timeouts.
Each call defaults to 3 seconds, including the body download; the checkout defaults to 15 seconds. Responses
are capped at 64 KiB; automatic redirects/cookies and retries are disabled. Cancellation reaches HTTP and EF.
Invalid input returns 400; missing/insufficient products/stock 409; invalid successful payloads 502;
network/unexpected status/oversized body 503; dependency or overall timeout 504. Remote error bodies are not
forwarded. Cancellation of the caller is not mislabeled as a dependency timeout.

No write begins during remote checks. A timeout/disconnect at the later commit boundary can be ambiguous;
there is no creation idempotency yet, so automatically retrying could duplicate orders.

## Database changes and preservation

No migration/model change. All existing public data remained:
Catalog 2 categories/5 products; Inventory 5 items, 110 on-hand, zero reserved;
Ordering 2 orders/3 lines, Pending 1059.97 USD and Cancelled 79.99 USD; Identity zero application tables.
Each test host migrated its own disposable schema using its owning role. All test schemas were removed
at teardown; no public seed/reset was needed and no Docker volume was deleted.

## Verification: exact commands and actual results

```powershell
docker compose up -d --wait --wait-timeout 45
./scripts/Test-Infrastructure.ps1
dotnet build src/Services/Ordering/Ordering.Api --disable-build-servers -m:1 --nologo
./scripts/Test-All.ps1
dotnet build MicroShop.sln --no-restore --disable-build-servers -m:1 --nologo
./scripts/Test-Skeleton.ps1
```

- Both infrastructure containers healthy; 4 owner connections, 4 wrong-password denials and all 12 cross-service
  CONNECT denials passed. RabbitMQ management/AMQP checks passed without application messaging.
- API and complete solution builds: zero warnings and zero errors.
- Final full tests: **141 passed, 0 failed, 0 skipped**. Architecture 4; Catalog 9/17, Inventory 14/27,
  Ordering 25/45 unit/integration. The previous 108 tests remain, with the authorized HTTP surface update.
- Real Kestrel checkout proves 201/Location, merged lines, Catalog prices/totals, stored history and unchanged stock.
- Editing a test Catalog product changes the next order while preserving the earlier snapshot.
- Missing product, absent stock and combined insufficiency after an earlier valid item create no partial order.
- Invalid caller JSON/price/status submissions fail; bad dependency JSON, missing fields, wrong identity/currency,
  invalid precision/name, inconsistent/negative stock, wrong content type, null and oversized bodies are handled.
- Downstream 500/redirect, actual refused connection, slow headers, slow body, total deadline and caller abort
  are verified. Fault hosts confirm no retry/redirect and cancellation arrival; no order is saved.
- Seven-host smoke passed, including database GETs, Swagger and empty checkout 400; processes stopped.
- Read-only psql inspection verified the public counts/totals above and zero *_test_* schemas in all three databases.
- No browser automation is claimed. Local logs are ignored under artifacts/phase-06-*.log.
- Publication checks passed: six local passwords absent from publishable files, .env ignored/untracked,
  relative Markdown links resolve and git diff --check is clean.

## Problems resolved and decisions

Docker Desktop startup failed on inaccessible stale socket reparse files. Native PowerShell renamed the
socket-only Docker/run and docker-secrets-engine directories under LOCALAPPDATA with .stale-phase6 timestamps,
preserving their contents; after both stale paths were absent together, Desktop regenerated sockets and
started normally. No factory reset, credential change or database volume change was needed.

Initial integration hosting collided on the default port; explicit Kestrel Listen on loopback port 0 fixed it.
The deadline/caller-abort fixture initially reused its parent factory client URL; starting the derived server
before creating the client selects the newly bound URL. The Windows connection-refusal case initially hit its
500 ms timeout; giving OS refusal 5 seconds tests 503 distinctly from deliberate 504 scenarios. All corrections
are in the test harness, with the final full suite passing. Bounded MSBuild concurrency from Phase 5 is retained.
See [ASP.NET Core test-factory source](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Testing/src/WebApplicationFactory.cs)
for the factory hosting/client lifecycle. ADR-018 supersedes only ADR-017's Phase 5 read-only stopping point.

## Remaining limits and handoff

Availability is not reserved, even when two simultaneous checkouts pass. Price/stock reads are not an atomic
distributed snapshot. Orders remain Pending without confirmation, cancellation workflow, stock effects,
creation idempotency, messaging or Outbox/Inbox. Phase 9 must authenticate/enforce customer ownership;
Phase 11 coordinates reservations. Gateway routing is the next phase, not part of this implementation.
Mandatory handoff/roadmap/architecture/ADR/changelog and topic guides are updated. Stop before Phase 7.
