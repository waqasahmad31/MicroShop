# Phase 00 — Architecture and repository planning

Status: COMPLETED

## Goal and planned deliverables
Architecture, project tree and references, packages, ports, databases, Docker design, workflow and handoff.

## Definition of Done
Required planning documents exist and agree; environment inspected; no business functionality implemented.

## What / why / flow / concept
Defined the target architecture and incremental roadmap before creating application code.
The repository now carries the context needed by an agent with no chat history.
HTTP/events cross service boundaries while each service owns its persistence; diagrams are in ARCHITECTURE.md.
This demonstrates bounded contexts and clean dependency direction.

## Files changed
AGENTS.md and docs/{PROJECT_CONTEXT,ARCHITECTURE,IMPLEMENTATION_PLAN,CURRENT_STATUS,NEXT_STEPS,DECISIONS,CHANGELOG}.md;
all 19 phase records.

## Verification and inspection
Inspected workspace/ancestor instructions; no existing MicroShop or parent Git root.
dotnet --version: 10.0.302; dotnet new blazor --help and dotnet new sln --help succeeded.
NuGet index queried for exact skeleton versions. Architecture and package documentation consulted.
No application build was applicable before the skeleton.

## Problems and decisions
Windows dotnet --info workload inspection throws; verify actual build in Phase 1.
Docker Linux engine not running; infrastructure verification deferred to authorized Phase 2.
ADRs 001–012 record ownership, layering, messaging, topology, UI, auth and incremental scope.

## Database / migrations / APIs / tests
None added in planning.

## Remaining work
Create and verify Phase 1 skeleton; no business functionality yet.


