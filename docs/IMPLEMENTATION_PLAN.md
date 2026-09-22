# Implementation plan

Canonical roadmap. Last updated: 2026-09-22. Current authorization: Phases 0–1 only.
Status vocabulary: NOT STARTED, IN PROGRESS, COMPLETED, BLOCKED. Completed phases stay in this file.
Each phase requires relevant build/tests plus the end-of-phase documentation updates in AGENTS.md.

## Repository layout

```text
MicroShop/
  MicroShop.sln
  global.json / Directory.Build.props / AGENTS.md / README.md
  src/
    Gateway/MicroShop.Gateway
    Web/MicroShop.Web
    Web/MicroShop.Web.Client
    Services/
      Identity/Identity.Api, Identity.Application, Identity.Domain, Identity.Infrastructure
      Catalog/Catalog.Api, Catalog.Application, Catalog.Domain, Catalog.Infrastructure
      Inventory/Inventory.Api, Inventory.Application, Inventory.Domain, Inventory.Infrastructure
      Ordering/Ordering.Api, Ordering.Application, Ordering.Domain, Ordering.Infrastructure
      Notification/Notification.Service
    BuildingBlocks/Contracts, Messaging, Observability
  tests/MicroShop.Architecture.Tests
  docs/ (context, architecture, roadmap, status, next steps, ADRs, changelog, phases)
  docker-compose.yml / .env.example (Phase 2)
```

## Dependency map
For each of Identity/Catalog/Inventory/Ordering: Api -> Application + Infrastructure;
Application -> Domain; Infrastructure -> Application + Domain. Web -> Web.Client.
BuildingBlocks start with no references or speculative interfaces. Architecture tests inspect project files.
No cross-service project references. Future boundary extensions are documented in ARCHITECTURE.md.

## NuGet packages by project
All projects target net10.0. Exact direct versions are pinned when introduced.
Use matching stable 10.0.x Microsoft ASP.NET packages and EF/Npgsql provider major 10.

| Project(s) | Skeleton packages | Later packages |
|---|---|---|
| Identity/Catalog/Inventory/Ordering.Api | Microsoft.AspNetCore.OpenApi 10.0.12 | Microsoft.AspNetCore.Authentication.JwtBearer; Swashbuckle.AspNetCore.SwaggerUI |
| Domain and Application (each service) | None | Prefer none; add only when required |
| Infrastructure (each service) | None | Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Design (PrivateAssets=all), Npgsql.EntityFrameworkCore.PostgreSQL, Npgsql, Dapper |
| Identity.Infrastructure | None | Also Microsoft.AspNetCore.Identity.EntityFrameworkCore and System.IdentityModel.Tokens.Jwt |
| MicroShop.Gateway | Yarp.ReverseProxy 2.3.0 | No speculative package additions |
| MicroShop.Web | Microsoft.AspNetCore.Components.WebAssembly.Server 10.0.12 | Only host requirements as introduced |
| MicroShop.Web.Client | Microsoft.AspNetCore.Components.WebAssembly 10.0.12 | MudBlazor; Microsoft.Extensions.Http; Microsoft.AspNetCore.Components.WebAssembly.Authentication if used by selected auth implementation |
| Contracts | None | None |
| Messaging | None | RabbitMQ.Client; Microsoft.Extensions.Hosting.Abstractions; Microsoft.Extensions.Options.ConfigurationExtensions as needed |
| Observability | None | OpenTelemetry.Extensions.Hosting, OpenTelemetry.Instrumentation.AspNetCore, OpenTelemetry.Instrumentation.Http, OpenTelemetry.Instrumentation.Runtime, OpenTelemetry.Exporter.OpenTelemetryProtocol; logging via OpenTelemetry SDK |
| Notification.Service | None | Messaging and Observability project references when needed |
| MicroShop.Architecture.Tests | Microsoft.NET.Test.Sdk 18.10.1; xunit 2.9.3; xunit.runner.visualstudio 4.0.0 | Feature test projects: Microsoft.AspNetCore.Mvc.Testing and actual dependency fixtures |

ASP.NET Core health checks, logging, configuration and ProblemDetails use the shared framework.
EF CLI tool is introduced with migrations and pinned in a local tool manifest.
Package versions above were checked against the NuGet flat-container index during Phase 0; restore/build
must confirm compatibility. Later packages are deliberately not installed now.

## Ports, databases and infrastructure
Web 5100; Gateway 5200; Identity 5210; Catalog 5220; Inventory 5230; Ordering 5240; Notification 5250.
PostgreSQL 5432: identity_db, catalog_db, inventory_db, ordering_db, separate restricted roles.
RabbitMQ AMQP 5672 and management 15672. Docker application hosts use internal 8080 in Phase 16.
Phase 2 uses only PostgreSQL/RabbitMQ containers. No Docker files or databases are created in Phase 1.
Final Docker topology contains four APIs, Notification, Gateway and Web; Client is delivered by Web.

## Development workflow
1. Read handoff and current phase; confirm authorized scope.
2. Use .NET 10 SDK and open MicroShop.sln in an IDE or use CLI.
3. Restore and build from repository root. Run tests relevant to the phase.
4. In Phase 1 run each host with its http launch profile in separate terminals; no DB/broker required.
5. In Phase 2 start dependencies, supply untracked development secrets and verify database isolation.
6. With persistence added: apply separate service migrations explicitly, seed development data, start APIs,
   then Gateway and Web. Document Swagger URLs as each API's UI is introduced.
7. Implement small increments, build after meaningful steps and fix failures before proceeding.
8. End every phase by updating roadmap, status, next steps, decisions, phase record and changelog; stop at authorized boundary.

## Phases

### Phase 00 — Architecture and repository planning
Status: **COMPLETED**

Deliverables: Architecture, project tree and references, packages, ports, databases, Docker design, workflow and handoff.

Definition of Done: Required planning documents exist and agree; environment inspected; no business functionality implemented.

Dependencies: Repository and requirement inspection.
Record: [phase-00-planning.md](phases/phase-00-planning.md)

### Phase 01 — Solution skeleton
Status: **NOT STARTED**

Deliverables: MicroShop.sln, 23 source projects, architecture test project, basic hosts, explicit references and initial README.

Definition of Done: Required projects exist; no cycles; dotnet restore/build/test pass; architecture diagram and all handoff updates complete.

Dependencies: Phase 00 completed.
Record: [phase-01-skeleton.md](phases/phase-01-skeleton.md)

### Phase 02 — Development infrastructure
Status: **NOT STARTED**

Deliverables: PostgreSQL and RabbitMQ Compose services; environment configuration; four database roles/databases; local connection setup.

Definition of Done: Docker services healthy; all owner connections work; cross-database connections denied; persistence and clean-start instructions verified.

Dependencies: Phase 01 completed.
Record: [phase-02-infrastructure.md](phases/phase-02-infrastructure.md)

### Phase 03 — Catalog microservice
Status: **NOT STARTED**

Deliverables: Products/categories, validation, EF DbContext/configurations/migrations and writes, Dapper pagination/search/details, OpenAPI UI and development seed data.

Definition of Done: Create/update/delete/query and invalid-input tests pass; migrations apply to catalog_db only; SQL parameterized; endpoint docs accurate.

Dependencies: Phase 02 completed.
Record: [phase-03-catalog.md](phases/phase-03-catalog.md)

### Phase 04 — Inventory microservice
Status: **NOT STARTED**

Deliverables: InventoryItem, on-hand/reserved/available invariants, stock adjustments, EF writes, Dapper reads, migrations and deterministic seeds.

Definition of Done: Adjustments and reads work; negative/invalid values rejected; concurrent adjustments tested; no access to other databases.

Dependencies: Phase 03 completed.
Record: [phase-04-inventory.md](phases/phase-04-inventory.md)

### Phase 05 — Ordering microservice
Status: **NOT STARTED**

Deliverables: Order/OrderItem/status, price/name snapshots, EF persistence, Dapper order details/history, application interfaces for later HTTP clients.

Definition of Done: Domain and persistence tests pass; migrations isolated; totals and transitions verified. Public checkout must not accept unverified client prices; expose it when Phase 6 validation is wired.

Dependencies: Phase 04 completed.
Record: [phase-05-ordering.md](phases/phase-05-ordering.md)

### Phase 06 — Synchronous service communication
Status: **NOT STARTED**

Deliverables: Typed Catalog/Inventory clients with IHttpClientFactory, server-side prices, availability check and Pending order creation.

Definition of Done: End-to-end HTTP flow and missing products/insufficient stock/timeouts/cancellation verified; documented check-versus-reservation limitation; no RabbitMQ.

Dependencies: Phase 05 completed.
Record: [phase-06-synchronous-communication.md](phases/phase-06-synchronous-communication.md)

### Phase 07 — API Gateway
Status: **NOT STARTED**

Deliverables: YARP route groups for auth, catalog, inventory and orders; clear path forwarding.

Definition of Done: Catalog/Inventory/Ordering route tests pass; route configuration ready for Identity; no business logic in Gateway; unknown routes and service failures observable.

Dependencies: Phase 06 completed.
Record: [phase-07-gateway.md](phases/phase-07-gateway.md)

### Phase 08 — Blazor and MudBlazor
Status: **NOT STARTED**

Deliverables: Typed frontend clients; layout/navigation; product/category management, browsing, inventory, dashboard, orders, my-orders, cart and checkout.

Definition of Done: UI runs through Gateway; forms/loading/error/empty states work; no raw HTTP in Razor; no service implementation references; Pending checkout limitation visible.

Dependencies: Phase 07 completed.
Record: [phase-08-blazor.md](phases/phase-08-blazor.md)

### Phase 09 — Identity and security
Status: **NOT STARTED**

Deliverables: ASP.NET Core Identity, registration/login, JWT, rotating hashed refresh tokens/logout, Admin/Customer roles, Blazor auth state and guarded pages/APIs.

Definition of Done: Positive/negative auth tests pass, including ownership checks, invalid JWT, role escalation denial, refresh reuse/revocation and expiration; private keys kept outside source; HTTPS documented.

Dependencies: Phase 08 completed.
Record: [phase-09-identity-security.md](phases/phase-09-identity-security.md)

### Phase 10 — RabbitMQ fundamentals
Status: **NOT STARTED**

Deliverables: Direct RabbitMQ.Client, minimal IEventBus, sample event, durable exchange/queues, routing, confirms, manual ACK, bounded retry and dead-letter behavior.

Definition of Done: Publish/consume demonstrated including unavailable/unroutable broker and bad-message paths; no infinite requeue; documentation explains topology and delivery semantics.

Dependencies: Phase 09 completed.
Record: [phase-10-rabbitmq.md](phases/phase-10-rabbitmq.md)

### Phase 11 — Asynchronous order workflow
Status: **NOT STARTED**

Deliverables: OrderCreated/StockReserved/StockReservationFailed/OrderConfirmed/OrderRejected events; pending workflow; atomic reservations and explicit cancellation/release transitions.

Definition of Done: Order confirms or rejects through events; concurrent orders cannot oversell; repeated OrderId cannot double-reserve/release; cancelled order cannot reconfirm; DB/publish gap documented for Phase 13.

Dependencies: Phase 10 completed.
Record: [phase-11-async-ordering.md](phases/phase-11-async-ordering.md)

### Phase 12 — Notification service
Status: **NOT STARTED**

Deliverables: Consume confirmation/rejection into structured notification logs with event/order/correlation IDs.

Definition of Done: Both outcomes observable through independent queues; failures bounded; best-effort log persistence limitation explicit; no external email.

Dependencies: Phase 11 completed.
Record: [phase-12-notifications.md](phases/phase-12-notifications.md)

### Phase 13 — Outbox pattern
Status: **NOT STARTED**

Deliverables: Ordering transactional Outbox and dispatcher, then Inventory Outbox for outcome/release events; publish confirms before processed marking.

Definition of Done: Broker outage and restart recover committed pending events; order/stock and event writes are atomic locally; duplicate-delivery window demonstrated; no distributed transactions.

Dependencies: Phase 12 completed.
Record: [phase-13-outbox.md](phases/phase-13-outbox.md)

### Phase 14 — Idempotency and Inbox
Status: **NOT STARTED**

Deliverables: Processed EventId tracking in the same transaction as consumer state changes and outgoing events; delivery ACK after commit.

Definition of Done: Duplicate deliveries and crash-before-ACK do not repeat state changes; concurrent duplicate handling tested; Notification logging durability limitation documented.

Dependencies: Phase 13 completed.
Record: [phase-14-inbox.md](phases/phase-14-inbox.md)

### Phase 15 — Observability
Status: **NOT STARTED**

Deliverables: Structured logs and trace context, ASP.NET/HTTP/runtime telemetry, explicit RabbitMQ propagation, OTLP configuration, /health and live/ready checks.

Definition of Done: HTTP and async traces correlated; useful metrics visible; dependency failure distinguishes readiness from liveness; no secrets logged; viewer choice documented.

Dependencies: Phase 14 completed.
Record: [phase-15-observability.md](phases/phase-15-observability.md)

### Phase 16 — Dockerization
Status: **NOT STARTED**

Deliverables: Dockerfiles for seven hosts, application Compose services and DNS/configuration, Web-hosted client assets, migrations/startup procedure.

Definition of Done: Configured fresh checkout runs via docker compose up --build; full order/login flow, health, persistence and restart verified; localhost-only development exposure documented.

Dependencies: Phase 15 completed.
Record: [phase-16-dockerization.md](phases/phase-16-dockerization.md)

### Phase 17 — Testing consolidation
Status: **NOT STARTED**

Deliverables: Meaningful domain, use-case, API, role/ownership, stock race, event and outage/recovery tests; deterministic fixtures.

Definition of Done: dotnet test passes; infrastructure prerequisites explicit; create/update product, adjustment, checkout, insufficient stock, event duplicate and auth scenarios covered; no arbitrary coverage target.

Dependencies: Phase 16 completed.
Record: [phase-17-tests.md](phases/phase-17-tests.md)

### Phase 18 — Final documentation
Status: **NOT STARTED**

Deliverables: README and all topic guides, Mermaid flows, operating commands, teaching walkthroughs and accurate handoff.

Definition of Done: Another developer can run prerequisites, migrations, app and main flows using docs alone; every phase explains what/why/how/concept; limitations and statuses match implementation.

Dependencies: Phase 17 completed.
Record: [phase-18-documentation.md](phases/phase-18-documentation.md)

## Risks and trade-offs
- Four layers increase project count; retain them as a teaching aid, not an excuse for generic abstractions.
- Synchronous availability is a point-in-time check; no guaranteed reservation until the async phase.
- Basic messaging is not commit-safe until Outbox and not generally duplicate-safe until Inbox.
- Stock invariants still require atomic updates and OrderId-level reservation protection before generic Inbox.
- Early APIs/UI are unauthenticated local exercises until Phase 9. JWTs/private keys must never enter logs/public assets.
- WASM initial load is larger, and disabling prerendering delays initial content; it simplifies browser-only state.
- Separate processes and ports increase local startup effort; Compose simplifies that in the planned stages.
- Initial environment inspection recorded a workload-metadata diagnostic error and an unavailable Docker engine; verify actual build and infrastructure in their phases.
- Latest package versions do not prove compatibility; actual restore/build/test is the acceptance evidence.
- Notification logs are best-effort; no persistent/exactly-once notification guarantee is claimed.

## Documentation delivery map
Existing: ARCHITECTURE plus context, plan, status, next steps, ADRs, changelog and phase records.
Add topic guides when their implementation can be described accurately:
- Phase 3: database-ownership.md, efcore-vs-dapper.md.
- Phase 6: request-flow.md, synchronous-communication.md.
- Phase 9: authentication-flow.md.
- Phase 10: rabbitmq.md.
- Phase 11: asynchronous-communication.md.
- Phase 13: outbox-pattern.md.
- Phases 2/16: docker.md (infrastructure first, full system later).
Phase 18 audits all guides rather than generating them only at the end.

