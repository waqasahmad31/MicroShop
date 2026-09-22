# Changelog

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
