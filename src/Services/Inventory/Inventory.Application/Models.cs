namespace Inventory.Application;

public sealed record CreateInventoryItemRequest(Guid? ProductId, int? OnHand);
public sealed record AdjustStockRequest(int? Delta);
public sealed record InventoryItemDto(Guid ProductId, int OnHand, int Reserved, int Available);
public sealed record InventoryPage(IReadOnlyList<InventoryItemDto> Items, long TotalCount, int Page, int PageSize);
