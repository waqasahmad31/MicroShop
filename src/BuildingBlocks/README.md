# BuildingBlocks boundaries

These are empty libraries in Phase 1, with no service references or speculative types.
- MicroShop.Contracts: integration event contracts only, introduced in Phase 10. Never shared domain entities or a shared REST model.
- MicroShop.Messaging: small native RabbitMQ transport implementation, introduced in Phase 10. No business workflows.
- MicroShop.Observability: common telemetry/logging/health configuration, introduced in Phase 15. No service-specific policies.

Add references only when a real feature uses them. See docs/ARCHITECTURE.md and ADR-007.

