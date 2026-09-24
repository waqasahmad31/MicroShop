namespace Ordering.Application;

// Infrastructure implements these contracts over HTTP using local wire DTOs, never service project references.
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
