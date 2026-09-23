using Inventory.Domain;

namespace Inventory.Application;

public sealed class InventoryService(IInventoryReader reader, IInventoryWriter writer)
{
    public async Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest? request, CancellationToken ct)
    {
        if (request is null) throw new InventoryValidationException("Body", "A JSON request body is required.");
        if (request.OnHand is null) throw new InventoryValidationException("OnHand", "On-hand quantity is required.");
        var item = new InventoryItem(request.ProductId ?? Guid.Empty, request.OnHand.Value);
        await writer.CreateAsync(item, ct);
        return new(item.ProductId, item.OnHand, item.Reserved, item.Available);
    }

    public async Task<InventoryItemDto> AdjustAsync(Guid productId, AdjustStockRequest? request, CancellationToken ct)
    {
        ValidateId(productId);
        if (request?.Delta is null || request.Delta == 0)
            throw new InventoryValidationException("Delta", "A nonzero integer delta is required.");
        // The writer invokes the Domain rule on a locked, current row; no stale read-before-write.
        return await writer.AdjustAsync(productId, request.Delta.Value, ct)
            ?? throw new InventoryNotFoundException(productId);
    }

    public async Task<InventoryItemDto> GetAsync(Guid productId, CancellationToken ct)
    {
        ValidateId(productId);
        return await reader.GetAsync(productId, ct) ?? throw new InventoryNotFoundException(productId);
    }

    public Task<InventoryPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) throw new InventoryValidationException("Page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100)
            throw new InventoryValidationException("PageSize", "Page size must be between 1 and 100.");
        return reader.ListAsync(page, pageSize, ct);
    }

    private static void ValidateId(Guid id)
    {
        if (id == Guid.Empty) throw new InventoryValidationException("ProductId", "Product ID cannot be empty.");
    }
}
