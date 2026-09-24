# Request flow: synchronous checkout

This is the implemented Phase 6 flow. Gateway/browser checkout arrives in Phases 7/8.

```mermaid
sequenceDiagram
    participant C as Caller / Swagger
    participant O as Ordering API + CheckoutService
    participant P as Catalog API
    participant V as Inventory API
    participant D as ordering_db
    C->>O: POST /api/orders (customerId, productId/quantity lines)
    O->>O: Validate all input; merge duplicate product IDs
    loop Each distinct product
        O->>P: GET /api/catalog/products/{id}
        P-->>O: Current name, USD price (own catalog_db)
        O->>O: Validate response identity/price/currency
        O->>V: GET /api/inventory/items/{id}
        V-->>O: OnHand, Reserved, Available (own inventory_db)
        O->>O: Validate response and combined quantity
    end
    alt All checks pass before cancellation/deadline
        O->>D: EF transaction: Pending order + immutable snapshots
        D-->>O: Commit
        O-->>C: 201 Created + Location + order detail
    else Invalid/missing/insufficient/dependency failure
        O-->>C: ProblemDetails; no order saved during checks
    end
    C->>O: GET Location / customer history
    O->>D: Parameterized Dapper query of stored snapshots
    D-->>O: Order details / page
    O-->>C: 200 JSON
```

API handles HTTP binding, the overall deadline and ProblemDetails. Application owns input validation
and checkout coordination. Infrastructure owns typed HTTP adapters and EF/Dapper. Domain protects
aggregate invariants. Catalog and Inventory share neither DbContexts nor domain entities with Ordering.

The request makes no Inventory writes, reserves no stock and publishes no event. Prices are snapshots of
individual reads, not a distributed transaction. Dependency calls finish before local persistence begins.
Cancellation or response loss around the commit boundary can still leave an existing order; do not retry
automatically. Read the [communication guide](synchronous-communication.md) for exact errors and commands.
