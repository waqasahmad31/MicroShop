namespace Ordering.Domain;

public sealed class Order
{
    public const int MaximumLines = 100;
    private readonly List<OrderItem> _items = [];
    private Order() { }

    public Order(Guid id, Guid customerId, DateTime createdAtUtc, IReadOnlyList<OrderItem>? items)
    {
        if (id == Guid.Empty) throw new OrderingValidationException("Id", "Order ID cannot be empty.");
        if (customerId == Guid.Empty) throw new OrderingValidationException("CustomerId", "Customer ID cannot be empty.");
        if (createdAtUtc == default || createdAtUtc.Kind != DateTimeKind.Utc)
            throw new OrderingValidationException("CreatedAtUtc", "Creation time must be a non-default UTC timestamp.");
        if (items is null || items.Count is < 1 or > MaximumLines || items.Any(item => item is null))
            throw new OrderingValidationException("Items", "An order must contain 1–100 non-null input lines.");

        Id = id;
        CustomerId = customerId;
        CreatedAtUtc = createdAtUtc;
        foreach (var group in items.GroupBy(x => x.ProductId))
        {
            var first = group.First();
            if (group.Any(x => x.ProductName != first.ProductName || x.UnitPrice != first.UnitPrice))
                throw new OrderingValidationException("Items", "Repeated product IDs must have identical name and price snapshots.");
            // At most 100 lines of 1000 units each: Sum cannot overflow int; the item limit still applies.
            _items.Add(new OrderItem(id, first.ProductId, first.ProductName, first.UnitPrice, group.Sum(x => x.Quantity)));
        }
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? StatusReason { get; private set; }
    public string Currency => "USD";
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(x => x.LineTotal);

    public void TransitionTo(OrderStatus target, string? reason = null)
    {
        if (!Enum.IsDefined(target) || target == OrderStatus.Pending)
            throw new OrderTransitionException("An order cannot transition to that status.");
        // Repeated delivery of the same outcome does not overwrite the first recorded reason.
        if (target == Status) return;
        var allowed = Status == OrderStatus.Pending ||
            (Status == OrderStatus.Confirmed && target == OrderStatus.Cancelled);
        if (!allowed) throw new OrderTransitionException($"Cannot change an order from {Status} to {target}.");

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (normalizedReason?.Length > 500 || (target == OrderStatus.Rejected && normalizedReason is null))
            throw new OrderingValidationException("Reason", "Rejection requires a reason; reasons cannot exceed 500 characters.");
        if (target == OrderStatus.Confirmed && normalizedReason is not null)
            throw new OrderingValidationException("Reason", "Confirmation does not accept a failure or cancellation reason.");
        Status = target;
        StatusReason = normalizedReason;
    }
}
