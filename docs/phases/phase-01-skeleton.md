# Phase 01 — Solution skeleton

Status: COMPLETED

## Goal and planned deliverables
MicroShop.sln, 23 source projects, architecture test project, basic hosts, explicit references and initial README.

## Definition of Done
Required projects exist; no cycles; dotnet restore/build/test pass; architecture diagram and all handoff updates complete.

## What was implemented
MicroShop.sln with 23 source projects and one xUnit project, pinned .NET SDK and NuGet versions,
central target framework/nullable/implicit-usings properties, Git ignores and line-ending settings.
Four service APIs, Notification and Gateway each return identification JSON; four APIs expose Development OpenAPI.
Blazor Web hosts its Client project with explicit Interactive WebAssembly and disabled prerendering.
No business functionality was implemented.

## Why and concept
Establish independently runnable process boundaries and compile-time Clean Architecture direction
before adding persistence, distributed calls or UI features.
This demonstrates that a microservice owns its implementation while contracts, not implementation references,
will connect services at runtime.

## Request/data flow
GET a service root -> ASP.NET Core -> host identification JSON.
GET /openapi/v1.json in Development -> generated document describing the root endpoint.
GET Web root -> HTML/WebAssembly descriptor -> browser downloads the Blazor client application.
The client landing page displays the runtime name; no API clients/calls exist yet.
YARP loads an empty route configuration; /api/catalog/products correctly returns 404 in this phase.
There are no database connections or inter-service network calls.

## Projects/files and important entry points
See ../PROJECT_STRUCTURE.md for the full file inventory and actual reference map.
Program.cs in each host is its composition root. Web Components/App.razor selects render mode;
Client Routes.razor and Pages/Home.razor supply the skeleton UI.
ProjectBoundaryTests contains four architecture checks; scripts/Test-Skeleton.ps1 checks host startup.
No placeholder domain classes, use-case interfaces or message contracts were introduced.

## Database changes / migrations / APIs
No databases or migrations. Only root host-identification endpoints and Development OpenAPI JSON.
No Swagger UI, health endpoints, business APIs or Gateway service routes.

## Tests and verified commands
From repository root:
- dotnet restore MicroShop.sln: exit 0, all 24 projects restored.
- dotnet build MicroShop.sln --no-restore: exit 0, 0 warnings, 0 errors.
- dotnet test MicroShop.sln --no-build --no-restore: exit 0; 4 passed, 0 failed, 0 skipped.
- ./scripts/Test-Skeleton.ps1: exit 0; Identity, Catalog, Inventory, Ordering, Notification,
  Gateway and Web all passed. Checked API OpenAPI, unmapped Gateway route and Blazor bootstrap delivery.

Test coverage: solution project inventory, exact allowed reference graph, cycle detection and
Domain independence from project/package/framework references. No business tests apply yet.
Smoke logs are ignored under artifacts/skeleton-smoke. Launched hosts were stopped.
Browser execution was not automated; the check is explicitly HTTP/bootstrap delivery.

## Problems encountered
dotnet --info fails in Windows workload metadata; restore/build/test remain successful.
dotnet new emitted OS bind-symbol warnings; actual builds emitted none.
The Blazor template generated a nested solution; removed it so MicroShop.sln is the sole entry point.
Docker Linux engine was not available; no attempt to start Phase 2 infrastructure.

## Decisions
ADRs 005–012 govern empty layer boundaries, WebAssembly mode, staged packages, independent Git root,
no speculative abstractions and phase-appropriate tests. Gateway package is present; routes are deferred.
Default template instructional error text was replaced with a concise user error and request identifier.

## Remaining work and handoff
All authorized Phase 1 requirements met, including persistent documentation.
Phase 2 is recommended but NOT STARTED. See ../NEXT_STEPS.md.
No commits or remote were requested; repository files are unstaged.
