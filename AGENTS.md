# MicroShop agent entry point

Read before significant changes, in this order:
1. This file.
2. docs/PROJECT_CONTEXT.md
3. docs/ARCHITECTURE.md
4. docs/IMPLEMENTATION_PLAN.md
5. docs/CURRENT_STATUS.md
6. docs/DECISIONS.md
7. docs/NEXT_STEPS.md
8. docs/phases/phase-XX-name.md for the phase being worked on.

## Scope and non-negotiable rules
- This is a learning project. Favor understandable code and explicit flows over clever abstractions.
- Current authorization is Phases 0–1 only. Stop after the skeleton; later phases require an instruction to continue.
- Database per service: identity_db, catalog_db, inventory_db, ordering_db. Never query another service's database; never create cross-service foreign keys.
- EF Core primarily for commands/writes, Dapper primarily for queries/reads. Identity framework persistence is an appropriate exception.
- Use .NET 10, ASP.NET Core, Blazor Web App with Interactive WebAssembly, MudBlazor, YARP, PostgreSQL and native RabbitMQ.Client.
- Domain has no ASP.NET Core, EF, Dapper, broker or infrastructure dependencies.
- Application depends on its Domain. Infrastructure implements Application interfaces. API composes Application and Infrastructure.
- No project references between microservices. No shared domain entities. BuildingBlocks.Contracts holds integration events only.
- No business logic in controllers, Gateway or Razor presentation components. Typed clients encapsulate HTTP.
- No unnecessary architecture changes. Record meaningful choices and reasons in DECISIONS.md before changing direction.
- Do not add major technologies without a documented reason. No Kubernetes, Kafka, Redis, Elasticsearch, service mesh, Keycloak, GraphQL, event sourcing or distributed transactions. No MassTransit initially. No MediatR without a strong documented reason.
- No generic repositories that mirror DbSet; no placeholder use cases or speculative interfaces.
- Use async APIs, CancellationToken, strongly typed options, parameterized SQL and ProblemDetails.
- Keep secrets out of Git, public WebAssembly assets, documentation and logs. Seed credentials are development-only and supplied outside source.
- Build after meaningful implementation changes; fix compiler failures before proceeding.
- Add meaningful tests with each main feature. Do not count empty test runs as passing feature tests.

## End-of-phase handoff (mandatory)
Run relevant build/tests and update IMPLEMENTATION_PLAN, CURRENT_STATUS, NEXT_STEPS, the phase record, and CHANGELOG. Update ARCHITECTURE and DECISIONS when applicable.
Every completed phase explains WHAT, WHY, HOW the request/data flows, and the microservice concept demonstrated.
Record exact commands, actual results, environmental blockers, migrations, API changes, known limitations and remaining work.
Do not mark a phase completed while its documentation or required verification is incomplete.
Keep completed phases in the roadmap. Never silently reverse an ADR; supersede it with a new ADR.
Repository documentation is the persistent source of truth. Verify it against files before changes; do not depend on chat history.

