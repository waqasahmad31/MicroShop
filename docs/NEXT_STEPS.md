# Next steps

Phase 6 synchronous service communication is complete. Stop before Phase 7 until instructed to continue.
Aspire/Azure Phases 19–28 and Kubernetes/AKS remain future context only.

## Next implementation phase: Phase 7 — API Gateway

1. Read AGENTS.md and its prescribed handoff files, then docs/phases/phase-07-gateway.md.
2. Preserve existing service databases, seeds, ports and all 141 tests. PostgreSQL host port is 5433 here.
3. Configure the existing YARP Gateway on localhost:5200 for /api/auth, /api/catalog, /api/inventory and
   /api/orders, using service destinations 5210–5240. Preserve prefixes; service endpoints already use them.
4. Keep Gateway free of checkout/business rules. Ordering continues its own typed internal Catalog/Inventory
   calls; the gateway forwards browser/client requests and does not query service databases.
5. Verify proxied Catalog/Inventory/Ordering APIs, including checkout 201 + Location, validation/error responses,
   unknown routes and unavailable services. Keep auth route configuration ready for Phase 9's Identity API.
6. Preserve cancellation, finite dependency timeouts and Pending/no-reservation behavior. Do not blindly retry
   order creation. Keep service URLs configurable; do not introduce cloud/discovery/Compose application hosts.
7. Update Test-Skeleton's current assertion that gateway business routes return 404 when routing is added.
   Add meaningful forwarding/route tests and verify service failures remain observable without leaking internals.
8. Update mandatory roadmap, status, next steps, architecture/ADRs, phase record and changelog. Build/test/smoke,
   commit with a detailed project-focused message and push main, then stop before Phase 8.

No Blazor business UI, MudBlazor, authentication, RabbitMQ events, reservation workflow or cloud implementation
in Phase 7. The narrow Web-origin CORS policy belongs to browser HTTP integration in Phase 8 unless a verified
Phase 7 requirement needs it. Identity remains a skeleton, so route readiness is not authentication functionality.

## Existing run/test workflow

[Catalog](catalog.md), [Inventory](inventory.md), [Ordering](ordering.md) and
[synchronous communication](synchronous-communication.md) cover migrations, seeds, direct hosts and checkout.
Test-All privately prepares/restores three service test connections and runs 141 tests. Checkout tests host
three actual Kestrel APIs against independent disposable PostgreSQL schemas. No public development data writes.

```powershell
docker compose up -d --wait --wait-timeout 45
./scripts/Test-Infrastructure.ps1
dotnet build MicroShop.sln --no-restore --disable-build-servers -m:1
./scripts/Test-All.ps1
./scripts/Test-Skeleton.ps1
```

Skeleton smoke requires a built solution and existing migrations; it starts/stops seven hosts, verifies reads,
Swagger and invalid checkout, without requiring Catalog/Inventory to remain up together. The integration suite
verifies the simultaneous three-service flow. Application hosts stop after checks; containers remain available.
No volume resets for ordinary startup or tests. See CURRENT_STATUS for the recovered Docker socket issue.
