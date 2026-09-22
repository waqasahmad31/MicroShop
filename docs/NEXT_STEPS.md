# Next steps

Phase 3 Catalog is complete. Stop before Phase 4 until the user instructs continuation.
Aspire/Azure Phases 19–28 and Kubernetes/AKS remain future context only.

## Next implementation phase: Phase 4 — Inventory

1. Read AGENTS.md, prescribed handoff files and docs/phases/phase-04-inventory.md.
2. Confirm dependencies with `docker compose up -d --wait --wait-timeout 120` and
   `./scripts/Test-Infrastructure.ps1`. Local PostgreSQL port is 5433; retain data.
3. Implement InventoryItem on-hand/reserved/available invariants, stock adjustments and concurrency
   handling in Inventory's four layers. Reject negative/invalid stock values.
4. Add Inventory EF writes/migration and Dapper reads using only inventory_db/inventory_app.
   `./scripts/Set-ServiceEnvironment.ps1 -Service Inventory` supplies that terminal's connection.
5. Seed matching Catalog product IDs from [catalog.md](catalog.md) in Inventory's own data;
   never query catalog_db, share Product entities or add cross-service references/foreign keys.
6. Test adjustments/reads/invalid input and real database concurrent adjustments.
   Follow Catalog's disposable-schema fixture pattern with Inventory's own configuration.
7. Preserve Catalog's 30 passing tests. Extend setup carefully: each fixture receives its own
   service connection rather than sharing one global connection string across services.
8. Build, apply only Inventory migration, verify HTTP/data flows and update mandatory handoff/ADR/phase docs.
   Commit the phase with a detailed project-focused message and push the existing main branch.

Do not introduce Ordering, Gateway routes, security, messaging or cloud work in Phase 4.
Do not reset volumes for startup/tests. `docker compose down` retains data; `down -v` deletes it.

## Continue using Catalog

Use [catalog.md](catalog.md) for startup, migrations/seed and API examples.
`./scripts/Test-Catalog.ps1` runs current tests against isolated PostgreSQL test schemas.
Application processes are stopped after verification; dependency containers remain running.
