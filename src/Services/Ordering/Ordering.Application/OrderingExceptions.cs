namespace Ordering.Application;

public sealed class OrderingNotFoundException(Guid orderId) : Exception($"Order '{orderId}' was not found.");
public sealed class OrderingConflictException(string message) : Exception(message);

public enum DependencyFailure { Unavailable, InvalidResponse, Timeout }
public sealed class OrderingDependencyException(string service, DependencyFailure failure)
    : Exception($"{service} could not complete the checkout dependency request.")
{
    public DependencyFailure Failure { get; } = failure;
}
