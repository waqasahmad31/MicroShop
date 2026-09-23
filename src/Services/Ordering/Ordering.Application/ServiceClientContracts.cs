namespace Ordering.Application;

// Phase 6 implements these contracts with typed HTTP clients. No stubs/fallback prices are registered.
public sealed record CatalogProductSnapshot(Guid ProductId, string Name, decimal UnitPrice, string Currency);
public sealed record InventoryAvailability(Guid ProductId, int Available);
public interface ICatalogServiceClient
{
    Task<CatalogProductSnapshot?> GetProductAsync(Guid productId, CancellationToken ct);
}
public interface IInventoryServiceClient
{
    Task<InventoryAvailability?> GetAvailabilityAsync(Guid productId, CancellationToken ct);
}
