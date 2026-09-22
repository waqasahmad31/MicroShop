# Changelog

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
