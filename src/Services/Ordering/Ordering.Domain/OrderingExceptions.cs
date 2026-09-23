namespace Ordering.Domain;

public sealed class OrderingValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
public sealed class OrderTransitionException(string message) : Exception(message);
