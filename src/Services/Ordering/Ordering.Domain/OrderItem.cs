namespace Ordering.Domain;

public sealed class OrderItem
{
    public const int MaximumQuantity = 1000;
    public const decimal MaximumUnitPrice = 99_999_999.99m;
    private OrderItem() { }
    public OrderItem(Guid productId, string? productName, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty) throw new OrderingValidationException("ProductId", "Product ID cannot be empty.");
        var name = productName?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 120)
            throw new OrderingValidationException("ProductName", "Product name must contain 1–120 characters.");
        if (unitPrice < 0 || unitPrice > MaximumUnitPrice || decimal.Round(unitPrice, 2) != unitPrice)
            throw new OrderingValidationException("UnitPrice", "Price must be 0–99,999,999.99 with at most two decimal places.");
        if (quantity is < 1 or > MaximumQuantity)
            throw new OrderingValidationException("Quantity", "Quantity must be between 1 and 1000.");
        ProductId = productId;
        ProductName = name;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    internal OrderItem(Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity)
        : this(productId, productName, unitPrice, quantity) => OrderId = orderId;

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
