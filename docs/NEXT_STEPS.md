# Next steps

Phase 2 is complete. Stop; do not start Phase 3 until the user instructs continuation.

## Next authorized milestone to request: Phase 3 — Catalog

1. Read AGENTS.md and the prescribed handoff, then docs/phases/phase-03-catalog.md.
2. Confirm infrastructure: docker compose up -d --wait --wait-timeout 120;
   ./scripts/Test-Infrastructure.ps1. Existing local PostgreSQL host port is 5433.
3. Introduce only Catalog persistence dependencies: EF Core 10, Npgsql EF provider/driver,
   Dapper and EF design-time tooling. Pin compatible versions; add a local dotnet-ef tool manifest.
4. Implement Category/Product and their invariants in Catalog.Domain; commands/queries/DTOs,
   validation and narrow interfaces in Catalog.Application. No generic DbSet-copy repositories.
5. In Catalog.Infrastructure implement CatalogDbContext/configurations, initial Catalog-only migration,
   EF writes and parameterized/paginated Dapper reads. Read ConnectionStrings:Database.
   ./scripts/Set-ServiceEnvironment.ps1 -Service Catalog supplies it for the current terminal.
6. Add thin Catalog API CRUD endpoints, ProblemDetails, cancellation, OpenAPI/Swagger UI and deterministic
   development seed data. Do not share Product entities with Inventory/Ordering.
7. Add meaningful tests for product/category creation, updates, invalid requests and read projections.
   Apply/verify the migration only in catalog_db, using catalog_app.
8. Verify restore/build/tests and Catalog HTTP flows, then update the handoff, phase record,
   database-ownership.md and efcore-vs-dapper.md before marking Phase 3 complete.

Likely projects: src/Services/Catalog/Catalog.{Domain,Application,Infrastructure,Api};
tests for Catalog; .config/dotnet-tools.json; docs.

Do not implement Inventory, Ordering, Gateway routes, authentication, MudBlazor,
RabbitMQ event infrastructure, Outbox/Inbox, OpenTelemetry or application Dockerfiles in Phase 3.
Do not reset volumes as a routine startup step. docker compose down preserves data; down -v deletes it.
