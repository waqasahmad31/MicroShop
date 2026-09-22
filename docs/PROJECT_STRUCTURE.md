# Phase 1 project and file inventory

Snapshot: 2026-09-22. 23 source projects, one xUnit project, 21 project-reference edges.
All files below were created for Phases 0–1. Generated bin/obj, Git internals and ignored smoke logs are excluded.

## Actual project references

| Project | Direct project references |
|---|---|
| MicroShop.Contracts | None |
| MicroShop.Messaging | None |
| MicroShop.Observability | None |
| MicroShop.Gateway | None |
| Catalog.Api | Catalog.Application, Catalog.Infrastructure |
| Catalog.Application | Catalog.Domain |
| Catalog.Domain | None |
| Catalog.Infrastructure | Catalog.Application, Catalog.Domain |
| Identity.Api | Identity.Application, Identity.Infrastructure |
| Identity.Application | Identity.Domain |
| Identity.Domain | None |
| Identity.Infrastructure | Identity.Application, Identity.Domain |
| Inventory.Api | Inventory.Application, Inventory.Infrastructure |
| Inventory.Application | Inventory.Domain |
| Inventory.Domain | None |
| Inventory.Infrastructure | Inventory.Application, Inventory.Domain |
| Notification.Service | None |
| Ordering.Api | Ordering.Application, Ordering.Infrastructure |
| Ordering.Application | Ordering.Domain |
| Ordering.Domain | None |
| Ordering.Infrastructure | Ordering.Application, Ordering.Domain |
| MicroShop.Web.Client | None |
| MicroShop.Web | MicroShop.Web.Client |
| MicroShop.Architecture.Tests | None |

## Files created

```text
.gitattributes
.gitignore
AGENTS.md
Directory.Build.props
docs/ARCHITECTURE.md
docs/CHANGELOG.md
docs/CURRENT_STATUS.md
docs/DECISIONS.md
docs/IMPLEMENTATION_PLAN.md
docs/NEXT_STEPS.md
docs/phases/phase-00-planning.md
docs/phases/phase-01-skeleton.md
docs/phases/phase-02-infrastructure.md
docs/phases/phase-03-catalog.md
docs/phases/phase-04-inventory.md
docs/phases/phase-05-ordering.md
docs/phases/phase-06-synchronous-communication.md
docs/phases/phase-07-gateway.md
docs/phases/phase-08-blazor.md
docs/phases/phase-09-identity-security.md
docs/phases/phase-10-rabbitmq.md
docs/phases/phase-11-async-ordering.md
docs/phases/phase-12-notifications.md
docs/phases/phase-13-outbox.md
docs/phases/phase-14-inbox.md
docs/phases/phase-15-observability.md
docs/phases/phase-16-dockerization.md
docs/phases/phase-17-tests.md
docs/phases/phase-18-documentation.md
docs/PROJECT_CONTEXT.md
docs/PROJECT_STRUCTURE.md
global.json
MicroShop.sln
README.md
scripts/Test-Skeleton.ps1
src/BuildingBlocks/Contracts/MicroShop.Contracts.csproj
src/BuildingBlocks/Messaging/MicroShop.Messaging.csproj
src/BuildingBlocks/Observability/MicroShop.Observability.csproj
src/BuildingBlocks/README.md
src/Gateway/MicroShop.Gateway/appsettings.json
src/Gateway/MicroShop.Gateway/MicroShop.Gateway.csproj
src/Gateway/MicroShop.Gateway/Program.cs
src/Gateway/MicroShop.Gateway/Properties/launchSettings.json
src/Services/Catalog/Catalog.Api/appsettings.json
src/Services/Catalog/Catalog.Api/Catalog.Api.csproj
src/Services/Catalog/Catalog.Api/Program.cs
src/Services/Catalog/Catalog.Api/Properties/launchSettings.json
src/Services/Catalog/Catalog.Application/Catalog.Application.csproj
src/Services/Catalog/Catalog.Domain/Catalog.Domain.csproj
src/Services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj
src/Services/Identity/Identity.Api/appsettings.json
src/Services/Identity/Identity.Api/Identity.Api.csproj
src/Services/Identity/Identity.Api/Program.cs
src/Services/Identity/Identity.Api/Properties/launchSettings.json
src/Services/Identity/Identity.Application/Identity.Application.csproj
src/Services/Identity/Identity.Domain/Identity.Domain.csproj
src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj
src/Services/Inventory/Inventory.Api/appsettings.json
src/Services/Inventory/Inventory.Api/Inventory.Api.csproj
src/Services/Inventory/Inventory.Api/Program.cs
src/Services/Inventory/Inventory.Api/Properties/launchSettings.json
src/Services/Inventory/Inventory.Application/Inventory.Application.csproj
src/Services/Inventory/Inventory.Domain/Inventory.Domain.csproj
src/Services/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj
src/Services/Notification/Notification.Service/appsettings.json
src/Services/Notification/Notification.Service/Notification.Service.csproj
src/Services/Notification/Notification.Service/Program.cs
src/Services/Notification/Notification.Service/Properties/launchSettings.json
src/Services/Ordering/Ordering.Api/appsettings.json
src/Services/Ordering/Ordering.Api/Ordering.Api.csproj
src/Services/Ordering/Ordering.Api/Program.cs
src/Services/Ordering/Ordering.Api/Properties/launchSettings.json
src/Services/Ordering/Ordering.Application/Ordering.Application.csproj
src/Services/Ordering/Ordering.Domain/Ordering.Domain.csproj
src/Services/Ordering/Ordering.Infrastructure/Ordering.Infrastructure.csproj
src/Web/MicroShop.Web.Client/_Imports.razor
src/Web/MicroShop.Web.Client/Layout/MainLayout.razor
src/Web/MicroShop.Web.Client/Layout/MainLayout.razor.css
src/Web/MicroShop.Web.Client/MicroShop.Web.Client.csproj
src/Web/MicroShop.Web.Client/Pages/Home.razor
src/Web/MicroShop.Web.Client/Pages/NotFound.razor
src/Web/MicroShop.Web.Client/Program.cs
src/Web/MicroShop.Web.Client/Routes.razor
src/Web/MicroShop.Web/appsettings.Development.json
src/Web/MicroShop.Web/appsettings.json
src/Web/MicroShop.Web/Components/_Imports.razor
src/Web/MicroShop.Web/Components/App.razor
src/Web/MicroShop.Web/Components/Pages/Error.razor
src/Web/MicroShop.Web/MicroShop.Web.csproj
src/Web/MicroShop.Web/Program.cs
src/Web/MicroShop.Web/Properties/launchSettings.json
src/Web/MicroShop.Web/wwwroot/app.css
tests/MicroShop.Architecture.Tests/MicroShop.Architecture.Tests.csproj
tests/MicroShop.Architecture.Tests/ProjectBoundaryTests.cs
```
