# MicroShop

A .NET 10 microservices learning project. Phase 0 defines the architecture; application implementation starts in Phase 1.
Read docs/IMPLEMENTATION_PLAN.md for the roadmap and AGENTS.md for repository working rules.

```mermaid
flowchart TD
    Browser[Blazor WebAssembly + MudBlazor] --> Gateway[YARP Gateway]
    Gateway --> Identity
    Gateway --> Catalog
    Gateway --> Inventory
    Gateway --> Ordering
    Identity --> IDB[(identity_db)]
    Catalog --> CDB[(catalog_db)]
    Inventory --> VDB[(inventory_db)]
    Ordering --> ODB[(ordering_db)]
    Ordering <--> RabbitMQ
    Inventory <--> RabbitMQ
    RabbitMQ --> Notification
```

Each service owns its database. EF Core handles writes and Dapper read projections.
Learn synchronous HTTP before asynchronous reservations, then Outbox/Inbox and observability.
No business functionality is implemented in this planning checkpoint.
