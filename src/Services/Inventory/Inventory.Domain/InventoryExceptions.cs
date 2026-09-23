namespace Inventory.Domain;

public sealed class InventoryValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

public sealed class StockConflictException(string message) : Exception(message);
