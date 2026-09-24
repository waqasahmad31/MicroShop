# Synchronous service communication

Phase 6 adds a real checkout request to Ordering. Its Application layer coordinates Catalog and Inventory
through narrow interfaces; Infrastructure implements them with typed IHttpClientFactory clients. Each
service still reads/writes only its own database. No REST DTO or domain project is shared between services.

## Run the lesson

Start dependencies with `docker compose up -d --wait`. Apply/seed the existing three service migrations
using the [Catalog](catalog.md), [Inventory](inventory.md) and [Ordering](ordering.md) guides. There is no
Phase 6 migration or new infrastructure. Build the solution, then run these in three separate terminals:

```powershell
# Terminal 1
./scripts/Set-ServiceEnvironment.ps1 -Service Catalog
dotnet run --project src/Services/Catalog/Catalog.Api --no-build --launch-profile http
# Terminal 2
./scripts/Set-ServiceEnvironment.ps1 -Service Inventory
dotnet run --project src/Services/Inventory/Inventory.Api --no-build --launch-profile http
# Terminal 3
./scripts/Set-ServiceEnvironment.ps1 -Service Ordering
dotnet run --project src/Services/Ordering/Ordering.Api --no-build --launch-profile http
```

Open Ordering Swagger at http://localhost:5240/swagger or submit this from another terminal:

```powershell
$checkout = @{
    customerId = '33333333-3333-3333-3333-333333333331'
    items = @(
        @{ productId = '22222222-2222-2222-2222-222222222221'; quantity = 1 },
        @{ productId = '22222222-2222-2222-2222-222222222223'; quantity = 2 }
    )
} | ConvertTo-Json -Depth 4
$order = Invoke-RestMethod 'http://localhost:5240/api/orders' -Method Post -ContentType 'application/json' -Body $checkout
$order
Invoke-RestMethod "http://localhost:5240/api/orders/$($order.id)"
```

With unchanged seed products/stock, this returns **201 Created**, a Location pointing to the GET route,
1059.97 USD total and **Pending** status. It saves a real order. Repeating the request creates another
order; creation has no idempotency key. Inventory stays unchanged. The supplied customer ID is a local
learning input, not authenticated identity or proof of ownership; Phase 9 replaces this trust boundary.
Gateway routes and browser checkout are later phases, so this lesson calls Ordering directly.

## Input and authority

`CheckoutRequest` accepts customerId and items containing productId/quantity only. Empty/missing IDs,
null items, missing/nonpositive quantities, over 100 input lines and over 1000 combined units per product
return 400 before network calls. Repeated IDs merge before price and availability reads. Unknown JSON
properties, including names, prices, totals and status, return 400 rather than silently accepting them.

`CheckoutService` calls Catalog GET /api/catalog/products/{id}, then Inventory GET /api/inventory/items/{id}
for each distinct product. Prices/names come from Catalog. HTTP adapters validate response identity,
USD currency, required fields, bounded name/price and Inventory arithmetic. Missing prices never become
zero accidentally; zero is allowed only when explicitly supplied by Catalog. The clients keep small local
wire DTOs and ignore unrelated downstream fields so additive service responses remain compatible.

After every product passes, the existing OrderingService creates one immutable Pending aggregate.
EF saves header/items atomically. The database transaction does not span any HTTP calls. Dapper later
reads stored snapshots, so changing Catalog prices affects future orders without rewriting history.

## Configuration and deadlines

Ordering appsettings.json has these non-secret defaults; override them with environment settings:

| Environment key | Default | Valid values |
|---|---|---|
| Services__Catalog__BaseUrl | http://localhost:5220 | HTTP(S) origin, no credentials/path/query/fragment |
| Services__Inventory__BaseUrl | http://localhost:5230 | HTTP(S) origin, no credentials/path/query/fragment |
| Services__Catalog__TimeoutMilliseconds | 3000 | 100–30000 |
| Services__Inventory__TimeoutMilliseconds | 3000 | 100–30000 |
| Services__CheckoutTimeoutMilliseconds | 15000 | 1000–60000 |

Options validate at startup. HttpClientFactory manages pooled handlers; typed clients encapsulate HTTP.
Requests are sequential for an explicit learning flow. The checkout deadline bounds the cumulative cost
of up to 200 reads and local persistence. Per-call timeouts cover headers AND the buffered response body.
Responses are capped at 64 KiB. Automatic redirects/cookies and automatic retries are disabled.
Cancellation flows from the caller through checkout, HTTP, EF and Dapper.

## Failure behavior

| Outcome | Ordering response | Persistence |
|---|---|---|
| Invalid input or unknown JSON properties | 400 validation ProblemDetails | No order |
| Catalog 404, Inventory 404, insufficient available units | 409 ProblemDetails | No order |
| Invalid successful payload, identity/currency/stock mismatch | 502 ProblemDetails | No order |
| Network failure, non-200/non-404, oversized response | 503 ProblemDetails | No order during checks |
| Dependency timeout / checkout deadline | 504 ProblemDetails | No order during checks; uncertain near commit |
| Caller disconnect | Cancellation propagated; response may not be deliverable | No order during checks; uncertain near commit |

Remote error bodies and internal exception messages are not forwarded. Responses include traceId.
No fallback prices/stock and no blind write retries exist. If cancellation or a lost response happens
around commit, the order may already exist; neither 504 nor a network failure proves rollback.

## What this teaches and what remains

An availability check is a point-in-time observation. Another buyer or adjustment can change it before
the order is saved, and sequential product reads are not one consistent distributed snapshot. Two orders
may both pass the same stock check. Pending therefore means recorded intent, not guaranteed fulfillment.
Phase 11 introduces atomic reservations and coordinated outcomes. No broker/event, stock decrement,
confirmation, public cancellation, Outbox/Inbox or production security is added here.

Run `./scripts/Test-All.ps1` for the complete suite. Checkout integration starts three real Kestrel hosts
on dynamic loopback ports using independent PostgreSQL schemas and restricted service roles. Fault hosts
exercise malformed responses, bad status, redirects, oversized bodies, connection failures, slow headers,
slow bodies and cancellation. All fixtures tear down their hosts/schemas; public development data remains.
The test project references API entry points solely for hosting; production service references stay isolated.
See [request flow](request-flow.md), [Phase 6 record](phases/phase-06-synchronous-communication.md) and ADR-018.
