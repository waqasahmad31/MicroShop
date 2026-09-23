namespace Inventory.Application;

public interface IInventoryReader
{
    Task<InventoryItemDto?> GetAsync(Guid productId, CancellationToken ct);
    Task<InventoryPage> ListAsync(int page, int pageSize, CancellationToken ct);
}
