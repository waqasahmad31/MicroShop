# Architecture decisions

Dates are recorded per entry; ADR-001–015 were recorded on 2026-09-22. Supersede decisions with a new ADR; preserve history.

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

## ADR-014 — Extend the roadmap after local fundamentals
Date: 2026-09-22.
Context: preserve long-term distributed-development and Azure learning for a developer without prior conversation context.
The local/core roadmap ends at Phase 18, while the implemented repository is complete only through Phase 2.
Decision: keep Phases 0–18 unchanged; append FUTURE / NOT STARTED Phases 19–28 for Aspire, Azure foundation,
ACR/Container Apps, Azure PostgreSQL, Service Bus, Key Vault/Managed Identity, Azure observability, Bicep/azd,
one CI/CD platform and advanced Azure services. Keep Kubernetes/AKS as a separate later track;
Dapr/service mesh remain potential later topics, not core dependencies.
Reason: learn explicit Docker/network/configuration/service boundaries before higher-level orchestration
and managed services, and preserve this order in the repository rather than relying on conversation history.
Alternatives considered: introduce Aspire/Azure immediately; replace the local roadmap with cloud work;
fold Kubernetes into the initial system; implement two CI/CD platforms at once. These obscure fundamentals
or add premature complexity and were rejected.
Consequences: Phase 3 Catalog remains the next implementation phase. Roadmap inclusion grants no authority
to install packages/create projects/provision resources. Future deployment preserves four database owners,
retains RabbitMQ when adding a Service Bus alternative, retains YARP when comparing API Management and
reuses OpenTelemetry. Local and cloud configuration stay distinct. Select the first CI/CD platform in Phase 27;
GitHub repository publication now does not make that choice or start that phase.
Relationship to prior decisions: extends ADR-007/010's learning and handoff scope, without superseding the
local architecture or ADR-001/003/004/009/013. No existing architecture decision is silently reversed.
Validation for this change: document consistency, phase status/order, preserved local phases and unchanged
non-documentation project files. No cloud implementation or Phase 3 business functionality is part of this update.

## ADR-015 — Catalog persistence, validation and verification
Date: 2026-09-22.
Context: implement the first business service while keeping Domain/Application independent and preserving
the four-layer learning architecture and separate database ownership.
Decision: Domain Product/Category enforce invariants; CatalogService coordinates use cases through
ICatalogReader and an atomic command-specific ICatalogWriter. Infrastructure supplies Dapper projections,
EF writes and an explicit migration. This is not a generic repository or a new framework.
Category names use trimmed invariant uppercase uniqueness; a database index handles concurrent duplicates.
Products require an existing category; a local FK restricts deletion, including validation/write races.
Price uses decimal/numeric(10,2), accepts zero, rejects excess precision/range and uses one USD currency.
PUT replaces validated fields with last-write-wins; absent/deleted rows return 404. Constraint conflicts
return 409; invalid input returns 400 ProblemDetails with traceId. Unknown failures expose no internal details.
Migration/seed commands are explicit; seed is Development-only, transactional, deterministic and preserves edits.
Real PostgreSQL integration tests use generated schemas and independent migration histories, then clean up.
Reason: make the request/data flow visible and test provider-specific SQL/constraints without touching local seed data.
Alternatives considered: DbContext in Application (couples the layer), generic repositories/CQRS framework
(unnecessary), in-memory-only integration tests (miss PostgreSQL behavior), automatic startup migration/seed
(hides database operations), hard deletes cascading from categories (surprising product loss).
Consequences: EF command lookups/seed checks are allowed; API query projections remain Dapper. Writes have no
optimistic version tokens. List count/page can drift under concurrent changes. Seed jobs run sequentially.
Tests require local PostgreSQL and schema-creation rights in catalog_db; an interrupted process can leave a
disposable schema to inspect/clean. Authentication remains Phase 9. Future stock ownership stays with Inventory.
Validation: 30 tests passed, including concurrent duplicate rejection, CRUD, input errors, literal search,
isolated migration and repeatable seed; all seven HTTP hosts passed. Migration touched Catalog only.

## ADR-016 — Inventory quantities and concurrent adjustments
Date: 2026-09-23.
Context: stock changes must not lose concurrent updates or reduce on-hand below reserved quantities.
Decision: Inventory owns one InventoryItem per external ProductId in inventory_db, with integer OnHand,
Reserved and derived Available. Enforce 0 <= Reserved <= OnHand <= Int32.MaxValue in Domain and database.
Create starts Reserved at zero; signed nonzero deltas add/remove on-hand. A negative delta is valid only
when sufficient available stock remains. Empty IDs, missing quantities and zero deltas are validation errors.
An adjustment starts a Read Committed transaction, loads the row through parameterized EF SELECT FOR UPDATE,
invokes the Domain method, saves with EF and commits before returning. Dapper handles read projections.
Stock conflicts return 409; invalid input 400; missing item 404. Duplicate ProductId is protected by the PK.
No Catalog lookup/FK/project reference: external ID validity is a future service communication responsibility.
Deterministic seed IDs match Catalog by documented convention; local stock starts 10/25/40/15/20.
Reason: serialize writers of the same item across API instances while preserving stock rules in Domain.
Alternatives: last-write-wins (loses deltas), process-local locks (not cross-instance), optimistic version
retries (more retry machinery), atomic SQL-only arithmetic (moves the central rule out of Domain).
Consequences: transactions must stay short; cancellation and command timeout bound blocked queries.
GET is a point-in-time observation, not a reservation. Reserved is represented/enforced but no reservation,
release, order/event workflow, ledger or idempotency key is introduced. Do not blindly retry an adjustment
after an uncertain response; it may already have committed. Phase 11 will add order-level reservations.
Tests use isolated schemas with service-specific connections. Validation: 71 total tests passed, including
32 increments across two hosts, competing deductions with zero/nonzero reserved stock and an observed
row-lock wait followed by validation against newly committed state. No lost changes, underflow or reserved
stock consumption. Full build and seven-host smoke checks passed; only Inventory received a new migration.

## ADR-017 — Ordering snapshots and read-only Phase 5 HTTP contract
Date: 2026-09-23.
Context: implement order domain/persistence before trustworthy Catalog/Inventory HTTP integration exists.
Decision: orders own immutable product name/price/quantity snapshots and external customer/product IDs.
Bound orders to 100 input lines and 1–1000 units per distinct product; merge duplicate IDs only when
their snapshots agree. Prices use USD decimal values with the same range/precision as the Catalog contract.
Totals are derived from immutable lines in Domain and SQL, avoiding independently mutable total columns.
Orders start Pending. Allow Pending -> Confirmed/Rejected/Cancelled, Confirmed -> Cancelled;
Rejected/Cancelled are terminal. Repeating the current non-Pending status is a no-op preserving its reason.
Rejection requires a bounded reason. Invalid transitions leave state unchanged. These are local state rules,
not a claim that stock was reserved/released. Later workflows must coordinate these changes with Inventory.
EF persists the aggregate atomically; status commands lock the current order row in a transaction before
Domain validation. Dapper supplies details and paginated customer history from ordering_db only.
Expose GET routes only in Phase 5. Trusted in-process creation accepts priced snapshots for seed/tests;
never bind that command to a public request. Define narrow ICatalogServiceClient/IInventoryServiceClient
contracts for Phase 6, without implementations, network calls or production fake prices/stock.
Reason: demonstrate aggregate consistency/history without allowing unverified client prices or implying checkout.
Alternatives: public create with submitted prices (breaks price authority), cross-database reads (breaks ownership),
early HTTP clients (Phase 6 scope), stored editable totals (can drift), automatic confirmation (no reservation yet).
Consequences: customer identity/ownership is not authenticated until Phase 9; current GETs are localhost learning.
No public create/cancel/status endpoints, reservation messaging, Outbox/Inbox or creation idempotency yet.
Explicit deterministic Development seed demonstrates pending/history states; it preserves existing orders.
Validation: 108 tests passed (37 new Ordering checks), including totals, snapshots, aggregate rollback,
status races, stale tracked-state refresh, customer history and absence of HTTP writes. Full build and
seven-host smoke checks passed. Ordering migration/seed touched only ordering_db. Unrestricted parallel
MSBuild exhausted local memory; bounded build concurrency resolved it without dropping test coverage.
