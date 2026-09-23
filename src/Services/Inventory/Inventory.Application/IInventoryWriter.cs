using Inventory.Domain;

namespace Inventory.Application;

// Atomic use-case persistence, not a generic repository or a DbSet facade.
public interface IInventoryWriter
{
    Task CreateAsync(InventoryItem item, CancellationToken ct);
    // Lock, validate the current entity, save and commit in one transaction.
    Task<InventoryItemDto?> AdjustAsync(Guid productId, int delta, CancellationToken ct);
}
