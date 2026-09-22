# Architecture decisions

All entries dated 2026-09-22. Supersede decisions with a new ADR; preserve history.

## ADR-001 — Database per service
Context: one development PostgreSQL server must not become a shared data model.
Decision: four databases and owner roles; no cross-service SQL or foreign keys.
Reason: ownership remains explicit and independently evolvable.
Alternatives: schema-per-service, shared database.
Consequences: HTTP/events carry data; consistency is local, not one distributed transaction.

## ADR-002 — EF writes and Dapper reads
Context: demonstrate two persistence approaches without a CQRS framework.
Decision: tracked EF commands and parameterized Dapper read DTOs inside each owner.
Reason: clear, small examples of change tracking versus explicit SQL.
Alternatives: EF-only, generic repositories, full CQRS.
Consequences: SQL and EF mappings must evolve together; Identity stores remain framework-managed.

## ADR-003 — Native RabbitMQ first
Context: broker mechanics are learning objectives.
Decision: RabbitMQ.Client, a small transport abstraction, explicit confirms/ACK/retry/dead-letter behavior.
Reason: avoid hiding delivery semantics.
Alternatives: MassTransit, Kafka.
Consequences: application owns broker lifecycle/error handling; Outbox/Inbox arrive after basic messaging.

## ADR-004 — Dedicated YARP gateway
Context: browser should not know service addresses.
Decision: YARP handles /api/* routing with no business use cases.
Reason: native .NET reverse proxy with configuration-driven routes.
Alternatives: calls directly to each API, custom proxy.
Consequences: another host, narrow CORS and bearer forwarding needed later.

## ADR-005 — Four layers; single Notification host
Context: user requested simplified Clean Architecture.
Decision: Api/Application/Domain/Infrastructure for four business services; one Notification.Service host.
Reason: teach dependency direction without four empty notification layers.
Alternatives: vertical slices in one assembly, four notification layers.
Consequences: 23 source projects; modest navigation overhead. No speculative classes in empty libraries.

## ADR-006 — Blazor Web App plus explicit WebAssembly
Context: client HTTP flow should be visible.
Decision: separate Web and Client projects; interactive routes use WebAssembly with prerendering disabled initially.
Reason: avoid duplicated browser-only state and service registration while learning.
Alternatives: Server, Auto, global prerendering.
Consequences: initial WASM download; typed clients added with UI features; MudBlazor deferred to Phase 8.

## ADR-007 — Incremental scope and dependencies
Context: request authorizes only planning and skeleton initially.
Decision: stop after Phase 1; defer Compose, database packages, MudBlazor, business logic and messaging to their phases.
Reason: buildable learning checkpoints, no large placeholder implementation.
Alternatives: install all packages and scaffold empty use cases at once.
Consequences: packages are introduced and pinned when used; early limitations remain documented.

## ADR-008 — Honest synchronous and reliability milestones
Context: check-then-create is not stock reservation; DB commits and broker publishes are not atomic.
Decision: synchronous lesson stores Pending orders; async lesson reserves stock atomically;
Outbox on both state-changing publishers and Inbox follow in their specified phases.
Reason: expose failure modes before solving them.
Alternatives: silently label stock checks as guaranteed confirmation, distributed transactions.
Consequences: early phases are intentionally incomplete reliability demonstrations. No exactly-once claim.

## ADR-009 — Development topology
Context: run locally without a cluster.
Decision: explicit ports 5100/5200/5210–5250; one PostgreSQL and one RabbitMQ container;
application Dockerfiles in Phase 16. Separate host/browser and container service URLs.
Reason: easy local debugging and visible network boundaries.
Alternatives: orchestration platforms, all-in-one process.
Consequences: ports may need local overrides; current Docker engine is offline, relevant in Phase 2.

## ADR-010 — Canonical docs and repository boundary
Context: workspace is a projects collection with no MicroShop or parent Git root; Windows is case-insensitive.
Decision: create MicroShop as its own Git repository; use docs/ARCHITECTURE.md only;
maintain an explicit handoff and all 19 phase records.
Reason: avoid touching other projects and avoid case-only architecture files.
Alternatives: scaffold in workspace root; duplicate lowercase architecture document.
Consequences: no initial commit or remote is required; docs and statuses are part of each phase's Done criteria.

## ADR-011 — Initial token lifecycle
Context: explicit JWT/refresh-token teaching requirement with a browser client.
Decision: short-lived JWT, asymmetric development signing, hashed rotating/revocable refresh tokens;
initial client tokens in memory. APIs independently validate and enforce roles.
Reason: visible bearer-token flow without persisting long-lived secrets in browser storage.
Alternatives: local/session storage, cookie BFF, external identity server.
Consequences: reload requires login; HTTPS and token-reuse tests are Phase 9 requirements.

## ADR-012 — Skeleton verification
Context: empty libraries have no business behavior to test.
Decision: xUnit verifies project inventory, allowed reference direction, cycles and framework-free Domain boundaries;
smoke-test runnable hosts separately.
Reason: these are real Phase 1 failure modes.
Alternatives: dummy assertion tests, premature domain tests.
Consequences: future intentional boundary changes require an ADR and corresponding test updates.


## ADR-013 — Concrete Phase 2 infrastructure and ownership
Date: 2026-09-22.
Context: implement the approved infrastructure phase without adding persistence code; host 5432 is already occupied.
Decision: pin official postgres:17.11-bookworm and rabbitmq:4.2.9-management images; use the default
Compose network, named volumes, localhost-only ports and unless-stopped restarts. Use a stable RabbitMQ
hostname. PostgreSQL bootstraps four non-superuser service owner logins and databases, revokes PUBLIC
access and grants each matching owner database/schema privileges. Keep microshop_admin bootstrap-only.
Create ignored .env with distinct generated development passwords; preserve the 5432 repository default
and override this machine to 5433. Prepare ConnectionStrings__Database via a script without new packages.
Reason: explicit, testable database ownership, reproducible dependency startup and preserved existing local services.
Alternatives considered: shared login/schema-only separation (weak boundary); stopping native PostgreSQL
(disrupts other work); application containers/ORM setup now (outside Phase 2); moving major image tags
(less repeatable); hardcoded container names (unneeded because Compose names are predictable).
Consequences: bootstrap runs only on empty volumes, so .env changes do not rotate persisted accounts;
service owners can migrate their own databases but not connect to another service database. An administrator
still has cluster-wide access. down keeps data; down -v destroys this project's volumes. Health checks alone
do not prove credentials/isolation, so a repeatable verifier makes actual TCP/SQL/HTTP/AMQP checks.
Validation: four own logins, four wrong-password denials, 12 cross-service denials; persistence and clean
initialization passed; both containers healthy. This refines ADR-001/009, without changing service architecture.
