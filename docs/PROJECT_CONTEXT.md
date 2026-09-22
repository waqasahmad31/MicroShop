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
Phases 0–1 are complete. The continuation request authorizes Phase 2 development infrastructure only.
Do not start Phase 3 automatically. Read CURRENT_STATUS and NEXT_STEPS for the verified stopping point.
