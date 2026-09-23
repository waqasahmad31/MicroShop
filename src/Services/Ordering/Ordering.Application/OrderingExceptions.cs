namespace Ordering.Application;

public sealed class OrderingNotFoundException(Guid orderId) : Exception($"Order '{orderId}' was not found.");
public sealed class OrderingConflictException(string message) : Exception(message);
