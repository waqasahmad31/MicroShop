# Next steps

Phase 5 Ordering is complete. Stop before Phase 6 until the user instructs continuation.
Aspire/Azure Phases 19–28 and Kubernetes/AKS remain future context only.

## Next implementation phase: Phase 6 — Synchronous service communication

1. Read AGENTS.md, prescribed handoff files and docs/phases/phase-06-synchronous-communication.md.
2. Verify infrastructure and preserve all existing service data. Local PostgreSQL host port is 5433.
3. Implement Ordering.Application's ICatalogServiceClient and IInventoryServiceClient with typed
   IHttpClientFactory clients in Ordering.Infrastructure. Use configurable direct service URLs, bounded
   timeouts, CancellationToken and explicit handling of 404/non-success/invalid downstream responses.
   Do not introduce project references or SQL access to Catalog/Inventory.
4. Expose checkout only after its validation is wired. The public item request contains product IDs and
   quantities, never trusted names/prices/totals/status. Aggregate repeated product IDs before price/stock
   checks; enforce the existing 100-line/1000-unit limits and USD contract. Fetch authoritative snapshots.
5. After validation, call the existing priced-line aggregate creation operation to save a Pending order.
   Inventory availability is a point-in-time check, not a reservation; no stock decrement or automatic
   confirmation. Customer identity remains explicitly local/unauthenticated until Phase 9 ownership checks.
6. Verify the complete HTTP flow and unavailable products, missing/insufficient stock, timeouts,
   cancellation and downstream failures. Never blindly retry order creation; avoid partially saved orders.
7. Preserve all 108 current tests. Extend real HTTP integration fixtures without sharing service databases,
   and retain isolated PostgreSQL test schemas. Use bounded builds to avoid observed memory exhaustion.
8. Add request-flow.md and synchronous-communication.md, update mandatory handoff/ADRs/phase records,
   verify full build/tests/smoke, then commit the phase with a detailed project-focused message and push main.

No RabbitMQ events, reservation/release workflow, Gateway routes, UI, authentication or cloud implementation
in Phase 6. Gateway routes are Phase 7; the current direct Ordering GET routes already use /api/orders.

## Existing run/test workflow

[Catalog](catalog.md), [Inventory](inventory.md) and [Ordering](ordering.md) cover migrations, seeds, starts
and Swagger. ./scripts/Test-All.ps1 prepares three private service connections and runs all 108 tests;
Test-Catalog.ps1 remains a compatibility wrapper. Use:
`dotnet build MicroShop.sln --no-restore --disable-build-servers -m:1`.
Test-Skeleton.ps1 requires all three migrations and a built solution; it starts/stops seven hosts.
Dependency containers remain running; no volume resets for startup/tests.
