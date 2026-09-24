# Changelog

## 2026-09-24 — Phase 6
- Added POST /api/orders accepting IDs/quantities only; reject unknown fields and aggregate duplicate products before reads.
- Fetch authoritative Catalog snapshots and Inventory availability through typed IHttpClientFactory clients.
- Validate downstream IDs, required fields, USD prices and stock arithmetic; save one Pending aggregate only after all checks.
- Added configurable origins, per-call/overall deadlines, body-size bounds, cancellation and sanitized 409/502/503/504 responses.
- Keep stock unchanged, no automatic retries/redirects, no transaction across HTTP calls, no new migration or messaging.
- Added four application and 29 real HTTP/PostgreSQL checks; all 141 tests pass with no failures/skips.
- Verified complete solution build, seven hosts, database ownership and preservation of development data.
- Recovered Docker Desktop startup by preserving/renaming stale socket-only runtime directories; no volume reset.
- Added request-flow and synchronous-communication guides, ADR-018 and updated handoff. Stop before Phase 7.

## 2026-09-23 — Phase 5
- Implemented Order/OrderItem, immutable name/price snapshots, exact derived totals and bounded quantities.
- Added duplicate-line aggregation, Pending/Confirmed/Rejected/Cancelled rules and guarded terminal states.
- Persisted aggregates atomically with EF; serialized status commands and refreshed stale tracked state.
- Added Dapper details/customer history, read-only HTTP endpoints, ProblemDetails and Development Swagger.
- Added Ordering-only migration and repeatable synthetic seed; prepared client interfaces for Phase 6 without HTTP implementations.
- Added 21 unit/application and 16 real PostgreSQL integration checks; all 108 solution tests pass.
- Resolved parallel MSBuild memory exhaustion by bounding build concurrency; final full build has zero warnings/errors.
- Verified seven hosts, blocked Ordering POST, aggregate rollback, state races, ownership and unchanged earlier-service seed data.
- Updated the learning guides, ADR-017 and handoff; stopped before public checkout/service communication in Phase 6.

## 2026-09-23 — Phase 4
- Implemented Inventory item creation, stock deltas and paginated/detail reads in its existing four layers.
- Enforced on-hand/reserved/available rules and overflow bounds; serialized adjustments with EF transactions/row locks.
- Added Inventory-only migration, deterministic stock seeds, ProblemDetails and Development Swagger/OpenAPI.
- Added 14 unit/application and 27 real PostgreSQL integration tests; all 71 solution tests pass.
- Verified concurrent updates across hosts, reserved-stock protection, lock waits, database constraints and seed preservation.
- Added service-specific test environment setup; preserved Catalog's test entry point and extended seven-host smoke checks.
- Verified migration ownership, seed repeatability, clean build/model checks and unchanged Catalog data without volume resets.
- Added Inventory learning guide and ADR-016; updated persistent handoff and stopped before Phase 5 Ordering.

## 2026-09-22 — Phase 3
- Implemented Catalog product/category CRUD, domain validation and application use cases.
- Added EF Core writes/migration, parameterized Dapper details/search/pagination and database constraints.
- Added ProblemDetails, Development Swagger UI and explicit repeatable seed with deterministic IDs.
- Applied migration only to catalog_db; seeded 2 categories/5 products without resetting volumes.
- Added 9 unit/application and 17 real PostgreSQL integration tests; all 30 tests including architecture pass.
- Verified full build with zero warnings/errors, model/migration agreement and all seven HTTP hosts.
- Added Catalog/data-ownership/EF-Dapper guides and ADR-015; updated persistent handoff; stopped before Phase 4.

## 2026-09-22 — Future learning roadmap (documentation only)
- Extended the canonical roadmap with FUTURE / NOT STARTED Aspire/Azure Phases 19–28.
- Separated local/core Phases 0–18 from cloud learning and a later Kubernetes/AKS track.
- Preserved database ownership, RabbitMQ and YARP comparison paths, and existing OpenTelemetry concepts.
- Recorded ADR-014; corrected stale scope/handoff wording while keeping Phases 0–2 complete and Phase 3 next.
- No application/cloud implementation, Azure packages/resources or Aspire projects introduced.

## 2026-09-22 — Phase 2
- Added PostgreSQL/RabbitMQ development Compose services with named volumes, health checks and localhost ports.
- Provisioned four restricted database owner logins and verified every cross-service connection is denied.
- Added local environment generation, service connection-string preparation and infrastructure verification helpers.
- Verified broker management/AMQP connectivity, persistence through recreation and fresh-volume initialization.
- Reverified .NET restore/build and four passing tests; left application projects unchanged.
- Updated the persistent handoff and Docker runbook; stopped before Phase 3 Catalog.

## 2026-09-22 — Phase 1
- Created MicroShop.sln with 23 source projects and one architecture test project on .NET 10.
- Added runnable API/Notification/Gateway hosts, Development OpenAPI documents and a Blazor WebAssembly skeleton.
- Enforced service dependency direction and Domain isolation with four passing architecture tests.
- Verified package restore, clean build and HTTP smoke checks for all seven hosts.
- Completed persistent handoff and all planned phase records; stopped before infrastructure implementation.

## 2026-09-22 — Phase 0
- Established service/data boundaries, solution structure, ordered roadmap and persistent AI/developer handoff.
- Documented HTTP, authentication and async order designs, staged reliability guarantees and architectural decisions.
