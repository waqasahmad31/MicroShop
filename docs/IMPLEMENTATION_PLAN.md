# Implementation plan

Canonical roadmap. Last updated: 2026-09-23. Phases 0–4 are complete; waiting to begin Phase 5.
Completed authorized work: Phase 4 Inventory. Stop before Phase 5 until instructed to continue.
Status vocabulary: NOT STARTED, IN PROGRESS, COMPLETED, BLOCKED. Completed phases stay in this file.
Each phase requires relevant build/tests plus the end-of-phase documentation updates in AGENTS.md.

## Learning tracks and sequencing

1. **LOCAL / CORE MICROSERVICES LEARNING — Phases 0–18:** the existing local roadmap remains unchanged.
2. **DISTRIBUTED DEVELOPMENT / AZURE LEARNING — Phases 19–28:** future context only, all NOT STARTED.
3. **SEPARATE ADVANCED TRACK — Kubernetes / AKS / related technologies:** future, separately scoped learning.

The intended progression is local fundamentals -> explicit microservice architecture -> Docker ->
observability -> .NET Aspire -> Azure managed services -> Infrastructure as Code -> CI/CD ->
advanced Azure services -> Kubernetes/AKS later.
Docker and observability develop incrementally in the existing phases; this summary does not renumber them.
**Phase 5 — Ordering Microservice is the next implementation phase.**
Future phases extend the project; they neither replace its local architecture nor authorize early cloud work.

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
  tests/MicroShop.Architecture.Tests, Catalog.Unit.Tests, Catalog.Integration.Tests
  tests/Inventory.Unit.Tests, Inventory.Integration.Tests
  .config/dotnet-tools.json (dotnet-ef 10.0.12)
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
must confirm compatibility. The table preserves the baseline/future map; Phase 3 adds these concrete packages:
Catalog.Infrastructure uses EF Core/Relational/Design 10.0.12 (Design private), Npgsql/provider 10.0.3
and Dapper 2.1.86. Catalog.Api adds SwaggerUI 10.2.3. Catalog.Integration.Tests uses Mvc.Testing 10.0.12;
both Catalog test projects use the existing xUnit/Test SDK versions. Domain/Application remain package-free.
Relational is pinned explicitly to avoid a transitive 10.0.4/10.0.12 assembly conflict.
Phase 4 Inventory uses the same versions for its Infrastructure, Swagger UI and two feature test projects.
Its Domain/Application remain package-free; no shared persistence project or cross-service reference was added.

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

## LOCAL / CORE MICROSERVICES LEARNING — Phases 0–18

### Phase 00 — Architecture and repository planning
Status: **COMPLETED**

Deliverables: Architecture, project tree and references, packages, ports, databases, Docker design, workflow and handoff.

Definition of Done: Required planning documents exist and agree; environment inspected; no business functionality implemented.

Dependencies: Repository and requirement inspection.
Record: [phase-00-planning.md](phases/phase-00-planning.md)

### Phase 01 — Solution skeleton
Status: **COMPLETED**

Deliverables: MicroShop.sln, 23 source projects, architecture test project, basic hosts, explicit references and initial README.

Definition of Done: Required projects exist; no cycles; dotnet restore/build/test pass; architecture diagram and all handoff updates complete.

Dependencies: Phase 00 completed.
Record: [phase-01-skeleton.md](phases/phase-01-skeleton.md)

### Phase 02 — Development infrastructure
Status: **COMPLETED**

Deliverables: PostgreSQL and RabbitMQ Compose services; environment configuration; four database roles/databases; local connection setup.

Definition of Done: Docker services healthy; all owner connections work; cross-database connections denied; persistence and clean-start instructions verified.

Dependencies: Phase 01 completed.
Record: [phase-02-infrastructure.md](phases/phase-02-infrastructure.md)

### Phase 03 — Catalog microservice
Status: **COMPLETED**

Deliverables: Products/categories, validation, EF DbContext/configurations/migrations and writes, Dapper pagination/search/details, OpenAPI UI and development seed data.

Definition of Done: Create/update/delete/query and invalid-input tests pass; migrations apply to catalog_db only; SQL parameterized; endpoint docs accurate.

Dependencies: Phase 02 completed.
Record: [phase-03-catalog.md](phases/phase-03-catalog.md)

### Phase 04 — Inventory microservice
Status: **COMPLETED**

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

## DISTRIBUTED DEVELOPMENT / AZURE LEARNING — Phases 19–28

All phases in this section are **FUTURE / NOT STARTED**. They extend the completed local curriculum;
their presence in the roadmap is not authorization to install tools/packages, add projects or provision resources.
Complete and understand Phases 0–18 before beginning Phase 19. Each later phase depends on the preceding
phase and an explicit instruction to proceed. Keep the working local implementation available for comparison.
Create a detailed phase record when that phase is authorized; the canonical planned scope is recorded here.

### Phase 19 — .NET Aspire / Distributed Application Development
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: .NET Aspire, AppHost, Service Defaults, resource orchestration, service discovery,
environment configuration, Aspire Dashboard, OpenTelemetry integration and the local distributed
application development experience.

Deliverables: an explicitly documented comparison between Docker Compose orchestration and an
Aspire-based development workflow for the existing services. Introduce AppHost/Service Defaults only then.

Definition of Done: explain how each resource, address, configuration value and telemetry path maps
to the already understood local system; demonstrate both development workflows and document trade-offs.
Aspire must not hide Docker, networking, configuration, service communication or microservice fundamentals.

Dependencies: Phases 0–18 completed and understood, including explicit Docker Compose and local observability.

### Phase 20 — Azure Foundation
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: Azure subscriptions, resource groups, regions, Azure CLI, Azure Developer CLI (azd),
Azure RBAC fundamentals, environments, naming conventions, tagging and basic Azure cost awareness.

Deliverables: a documented learning environment model, naming/tagging conventions, access boundaries,
cost/budget expectations and cleanup procedure.

Definition of Done: the learner can explain the basic resource model, scope of access, environment
separation and cost implications, and use CLI/azd for the approved learning exercises.
No application migration occurs until these fundamentals are understood.

Dependencies: Phase 19 completed and explicit authorization for Azure learning work.

### Phase 21 — Azure Container Registry and Azure Container Apps
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: Azure Container Registry (ACR), container image lifecycle, repositories, image tags,
pushing/pulling images; Azure Container Apps, environments, revisions, replicas, ingress, environment
variables, health probes, autoscaling fundamentals and service-to-service deployment concepts.

Deliverables: build/push/pull and deploy the existing containerized MicroShop services, with documented
ingress, internal communication, configuration and revision behavior. Preserve the local architecture.

Definition of Done: trace a versioned image from build to registry to Container Apps revision;
verify the deployment's ingress/probes/configuration and explain replicas/scaling and revision rollback.
Document staged dependency availability: managed PostgreSQL and alternative messaging are later lessons,
so successful container deployment alone does not claim a completed managed-cloud end-to-end workflow.
Do not redesign the application specifically for Azure.

Dependencies: Phase 20 completed; reuse the Phase 16 application containers.

### Phase 22 — Azure Database for PostgreSQL
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: Azure Database for PostgreSQL Flexible Server, connectivity, connection security,
database ownership, migrations, backups, basic networking and development versus cloud configuration.

Deliverables: a cloud database configuration and migration/backup learning exercise preserving
identity_db, catalog_db, inventory_db and ordering_db with separate service ownership.

Definition of Done: each service can authenticate/migrate only its own database; cross-service
access remains denied; verify secure connectivity and demonstrate/document backup and recovery behavior.
Explain configuration differences from the local PostgreSQL container.
Cloud deployment must not weaken logical database-per-service boundaries.

Dependencies: Phase 21 completed; existing per-service migrations and ownership rules remain authoritative.

### Phase 23 — Azure Service Bus
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: queues, topics, subscriptions, dead-letter queues, message locks/Peek-Lock,
retries, delivery semantics, message settlement, competing consumers, publish/subscribe and failure handling.

Deliverables: an alternative Azure Service Bus provider behind the messaging abstraction developed
in the RabbitMQ lessons. Target conceptual design (not code that exists today):

```text
IEventBus
  +-- RabbitMqEventBus
  +-- AzureServiceBusEventBus
```

Definition of Done: run equivalent application integration-event scenarios with each provider and
document semantic differences, duplicate delivery, retry/settlement and dead-letter behavior.
Retain RabbitMQ as the original/local learning implementation. Keep application-level event concepts
consistent where reasonable without pretending that the brokers have identical guarantees.
Preserve Outbox/Inbox reasoning and verify failure recovery with the alternative provider.

Dependencies: Phase 22 completed and the earlier RabbitMQ/Outbox/Inbox lessons understood.
Do not remove RabbitMQ merely because Service Bus is introduced.

### Phase 24 — Azure Key Vault and Managed Identity
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: Azure Key Vault, secrets, keys/certificates where relevant, Managed Identity,
DefaultAzureCredential, Azure RBAC, passwordless access to supported Azure resources and
local-development credentials versus cloud identities.

Deliverables: move appropriate cloud secrets/configuration away from plain environment secrets;
document supported identity-based access and least-privilege RBAC for the selected services.

Definition of Done: demonstrate cloud identity/secret access and denied access outside intended roles;
no secrets committed. Explain which settings remain non-secret configuration and which require protection.
Local development configuration and Azure production-like configuration remain clearly distinguished.
Select exact integrations/packages when this phase begins, not during roadmap maintenance.

Dependencies: Phase 23 completed. Earlier cloud exercises must still keep secrets out of source;
this phase deepens identity/secret-management learning rather than authorizing insecure earlier handling.

### Phase 25 — Azure Observability
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: Azure Monitor, Application Insights, Log Analytics, OpenTelemetry, distributed traces,
metrics, structured logs, correlation/trace IDs, dependency telemetry and KQL fundamentals.

Deliverables: reuse the local OpenTelemetry instrumentation and connect the approved Azure telemetry
pipeline; compare Aspire Dashboard for local development with Azure Monitor/Application Insights/
Log Analytics for hosted environments.

Definition of Done: follow an HTTP and asynchronous order flow using correlated telemetry, inspect
dependencies/metrics/logs and demonstrate useful KQL queries; document telemetry configuration and cost.
Do not create a separate, unrelated instrumentation approach.

Dependencies: Phase 24 completed; reuse the Phase 15 and Phase 19 observability concepts.

### Phase 26 — Infrastructure as Code
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: Bicep and Azure Developer CLI (azd), Infrastructure as Code, parameterized environments,
repeatable provisioning, resource dependencies, outputs, environment-specific configuration and
reproducible Azure environments.

Deliverables: codify the learned Azure environment in Bicep and azd. Suggested future structure only:

```text
infra/
  azure/
    main.bicep
    modules/
    environments/
```

Definition of Done: provision an approved environment repeatably from version-controlled definitions
and explicit parameters; document outputs, dependencies, environment differences and cleanup.
Manual Portal exploration may support learning, but the final environment must not depend solely
on manual Portal steps. Refine the exact file layout when this phase starts.

Dependencies: Phase 25 completed. No infra/azure files are created by this roadmap update.

### Phase 27 — CI/CD
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: choose one primary path, GitHub Actions OR Azure DevOps Pipelines; learn environment
separation, deployment secrets/identity, artifacts, deployment configuration and rollback/revisions.

Deliverables: automate this progression using the chosen platform:

```text
Source -> Restore -> Build -> Test -> Container Build -> Push to ACR
       -> Deploy to Azure Container Apps -> Post-deployment verification
```

Definition of Done: a reproducible pipeline builds/tests and deploys identifiable artifacts to the
intended environment, verifies deployment and demonstrates a documented rollback/revision procedure.
Use an appropriate deployment identity and keep secrets out of source/logs.
Record the platform choice in an ADR when this phase begins; no platform is selected now.
A second platform may be explored later for comparison, not implemented simultaneously at first.

Dependencies: Phase 26 completed; consume its repeatable infrastructure/environment configuration.

### Phase 28 — Advanced Azure Application Platform
Status: **NOT STARTED**
Classification: FUTURE.

Learning goals: Azure API Management, Microsoft Entra ID, Azure App Configuration, Feature Flags,
private networking fundamentals, Private Endpoints, scaling, resilience, security hardening and cost awareness.

Deliverables: bounded advanced learning exercises with documented responsibilities, trade-offs and
effects on the existing service boundaries; explore only after prior Azure phases are understood.

Definition of Done: document and verify the selected exercises, access/network boundaries, scaling/
resilience behavior and cost implications. Compare YARP and Azure API Management responsibilities/use cases.
Keep YARP initially; do not automatically replace it with API Management or silently redesign identity.
Any later architectural change requires its own explicit decision and verification.

Dependencies: Phases 19–27 completed and understood.

## SEPARATE ADVANCED TRACK — Kubernetes / AKS / related technologies
Status: **NOT STARTED**
Classification: FUTURE, separate from the core MicroShop implementation and Phases 19–28.

Only create/begin this advanced track after understanding:
- .NET microservices fundamentals and PostgreSQL ownership.
- EF Core + Dapper and synchronous service communication.
- RabbitMQ, asynchronous integration events and Outbox/Inbox.
- Docker and OpenTelemetry.
- .NET Aspire, Azure Container Apps and Azure Service Bus.
- Key Vault / Managed Identity, Bicep / azd and CI/CD.

Topics: Kubernetes fundamentals, AKS, Pods, Deployments, Services, namespaces, ConfigMaps, Secrets,
Ingress, persistent storage basics, readiness/liveness probes, requests/limits, autoscaling and Helm.
Dapr and service mesh are potential later topics, not prerequisites or additions to the core architecture.

Entry/exit expectations: begin only after the above prerequisites and a separate instruction;
produce a separately scoped plan, exercises and comparison to Container Apps. Do not imply an automatic
MicroShop migration to AKS. No AKS resources, manifests, Helm charts, Dapr or service mesh are introduced now.

## Risks and trade-offs
- Four layers increase project count; retain them as a teaching aid, not an excuse for generic abstractions.
- Synchronous availability is a point-in-time check; no guaranteed reservation until the async phase.
- Basic messaging is not commit-safe until Outbox and not generally duplicate-safe until Inbox.
- Stock invariants still require atomic updates and OrderId-level reservation protection before generic Inbox.
- Early APIs/UI are unauthenticated local exercises until Phase 9. JWTs/private keys must never enter logs/public assets.
- WASM initial load is larger, and disabling prerendering delays initial content; it simplifies browser-only state.
- Separate processes and ports increase local startup effort; Compose simplifies that in the planned stages.
- Phase 1 recorded a failing dotnet --info workload diagnostic; actual .NET verification passes.
  Docker is running in Phase 2. Native PostgreSQL owns host 5432; local .env uses 5433.
- Latest package versions do not prove compatibility; actual restore/build/test is the acceptance evidence.
- Notification logs are best-effort; no persistent/exactly-once notification guarantee is claimed.

## Documentation delivery map
Existing: ARCHITECTURE plus context, plan, status, next steps, ADRs, changelog and phase records.
Add topic guides when their implementation can be described accurately:
- Phase 3: database-ownership.md, efcore-vs-dapper.md.
- Phase 4: inventory.md, plus updates to database ownership, EF/Dapper and test setup guides.
- Phase 6: request-flow.md, synchronous-communication.md.
- Phase 9: authentication-flow.md.
- Phase 10: rabbitmq.md.
- Phase 11: asynchronous-communication.md.
- Phase 13: outbox-pattern.md.
- Phases 2/16: docker.md (infrastructure first, full system later).
Phase 18 audits all guides rather than generating them only at the end.
