# Project context

MicroShop is a small e-commerce/order management learning system, not a production deployment.
Its purpose is to make service boundaries, distributed request flows and failure modes understandable.

## Learning objectives
- Build independently owned Identity, Catalog, Inventory and Ordering services, plus a notification consumer.
- See EF Core writes and Dapper reads coexist within one service.
- Follow Blazor typed-client calls through YARP and service-to-service HTTP.
- Learn JWT validation, Identity roles and refresh-token lifecycle.
- Progress from synchronous stock checks to RabbitMQ reservation events, then Outbox and Inbox.
- Run locally and in Docker; understand structured logs, OpenTelemetry, health checks and useful tests.

## Stack
.NET 10 / ASP.NET Core 10; Blazor Web App with Interactive WebAssembly; MudBlazor;
PostgreSQL; EF Core 10; Dapper; YARP; ASP.NET Core Identity; JWT and refresh tokens;
RabbitMQ.Client; Docker Compose; OpenTelemetry; ASP.NET Core health checks/OpenAPI; xUnit.

## Domain
Admin manages products, categories and stock, and views orders/dashboard.
Customer browses products, manages a client-side cart, places orders and views their own orders.
Catalog owns price. Ordering stores product name and price snapshots. Inventory owns reservations.
Notification initially logs confirmed/rejected order events without a database or email provider.
Development seed identities: admin@microshop.local and customer@microshop.local.
Seed catalog: Laptop, Keyboard, Mouse, Monitor, Headphones; categories Computers and Accessories.
Future seed IDs must be deterministic across Catalog and Inventory without database joins.

## Constraints and philosophy
One database and database role per owning service. HTTP/events cross boundaries; SQL and domain entities do not.
Small explicit use cases and interfaces only when needed. Keep four layers in each main service as a teaching aid.
No generic repository, CQRS framework or premature resilience framework.
Excluded: Kubernetes, Kafka, Redis, Elasticsearch, service mesh, Keycloak, GraphQL, event sourcing,
distributed transactions; MassTransit initially; MediatR unless a strong architectural need emerges.
Business pages arrive in Phase 8, security in Phase 9, messaging in Phase 10, observability in Phase 15.
Earlier phases are local exercises with documented limitations, not secure or reliable deployment claims.

## Authorization and continuation
Phases 0–2 are complete. Current implementation state: waiting to begin Phase 3 — Catalog Microservice.
Current work is documentation-only roadmap maintenance and initial GitHub publication, not application/cloud implementation.
Do not start Phase 3 automatically. Read CURRENT_STATUS and NEXT_STEPS for the verified stopping point.

## Long-term learning roadmap (future context only)
- LOCAL / CORE MICROSERVICES LEARNING: Phases 0–18 retain the explicit local architecture.
- DISTRIBUTED DEVELOPMENT / AZURE LEARNING: Phases 19–28 are FUTURE / NOT STARTED:
  Aspire; Azure foundation; ACR/Container Apps; Azure PostgreSQL; Service Bus; Key Vault/Managed Identity;
  Azure observability; Bicep/azd; one CI/CD platform; advanced Azure application services.
- SEPARATE ADVANCED TRACK: Kubernetes/AKS, with Dapr/service mesh only as potential later topics.
  These remain excluded from the core MicroShop implementation.

Learn Compose and underlying Docker/network/configuration concepts before Aspire. Reuse the existing
service boundaries, four database owners and OpenTelemetry concepts in future cloud exercises.
Retain RabbitMQ while comparing an alternative Service Bus provider; retain YARP while comparing API Management.
Keep local configuration distinct from cloud production-like configuration, without committing secrets.
Choose GitHub Actions OR Azure DevOps first in Phase 27; initial GitHub repository hosting does not select a CI/CD platform.
The detailed goals, dependencies and Done criteria are in IMPLEMENTATION_PLAN.md; no future phase has begun.
