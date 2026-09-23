namespace Inventory.Domain;

public sealed class InventoryItem
{
    private InventoryItem() { }

    public InventoryItem(Guid productId, int onHand, int reserved = 0)
    {
        if (productId == Guid.Empty)
            throw new InventoryValidationException("ProductId", "Product ID cannot be empty.");
        if (onHand < 0)
            throw new InventoryValidationException("OnHand", "On-hand quantity cannot be negative.");
        if (reserved < 0 || reserved > onHand)
            throw new InventoryValidationException("Reserved", "Reserved quantity must be between zero and on-hand.");
        ProductId = productId;
        OnHand = onHand;
        Reserved = reserved;
    }

    public Guid ProductId { get; private set; }
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public int Available => OnHand - Reserved;

    public void AdjustOnHand(int delta)
    {
        if (delta == 0)
            throw new InventoryValidationException("Delta", "Adjustment must be a nonzero integer.");
        // Use a wider intermediate so overflow cannot turn a large addition into negative stock.
        var next = (long)OnHand + delta;
        if (next < Reserved)
            throw new StockConflictException("Adjustment would consume reserved stock or make stock negative.");
        if (next > int.MaxValue)
            throw new StockConflictException("Adjustment would exceed the maximum supported stock quantity.");
        OnHand = (int)next;
    }
}
