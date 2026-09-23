namespace Inventory.Application;

public sealed class InventoryNotFoundException(Guid productId)
    : Exception($"Inventory for product '{productId}' was not found.");
public sealed class InventoryConflictException(string message) : Exception(message);
