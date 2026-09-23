# Next steps

Phase 4 Inventory is complete. Stop before Phase 5 until the user instructs continuation.
Aspire/Azure Phases 19–28 and Kubernetes/AKS remain future context only.

## Next implementation phase: Phase 5 — Ordering

1. Read AGENTS.md, prescribed handoff files and docs/phases/phase-05-ordering.md.
2. Confirm infrastructure and preserve existing Catalog/Inventory data. Local PostgreSQL host port is 5433.
3. Implement Order/OrderItem/status, valid totals/transitions and product-name/price snapshots in Ordering's
   own layers. Keep server-authoritative pricing in mind: public checkout must not accept unverified prices.
4. Add EF writes/migration and Dapper order detail/history in ordering_db using ordering_app only.
   Prepare configuration with ./scripts/Set-ServiceEnvironment.ps1 -Service Ordering.
5. Define narrow application interfaces required for later Catalog/Inventory HTTP clients; actual typed HTTP
   communication and the public checkout flow are Phase 6. Do not claim stock reservation/confirmation.
6. Test domain totals/status transitions, snapshots and real database persistence; use service-specific
   disposable test schemas. Extend Test-All.ps1 for ORDERING_TEST_CONNECTION_STRING and preserve existing tests.
7. Verify migration ownership and full build/tests, update the handoff/phase record/ADR/guides, then create
   a detailed phase commit and push main. Do not implement Phase 6 without a continuation instruction.

No cross-service SQL, foreign keys, domain sharing or project references. OrderId-level reservations arrive
in Phase 11, with Outbox/Inbox later. Do not introduce Gateway routes, UI, security or messaging in Phase 5.

## Run the existing services

[Catalog guide](catalog.md) and [Inventory guide](inventory.md) contain explicit migrations, seed/start
commands, Swagger URLs and API contracts. ./scripts/Test-All.ps1 runs all 71 tests with separate database
connections; Test-Catalog.ps1 remains a compatibility wrapper for the full suite.
Test-Skeleton.ps1 requires both migrations and a built solution; it starts/stops the seven hosts.
Dependency containers remain running. Do not reset volumes for routine startup or tests.
