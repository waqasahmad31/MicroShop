using Ordering.Domain;

namespace Ordering.Application;

// Aggregate operations keep EF out of Application; no generic repository or mutable item API.
public interface IOrderingWriter
{
    Task CreateAsync(Order order, CancellationToken ct);
    Task<bool> TransitionAsync(Guid id, OrderStatus target, string? reason, CancellationToken ct);
}
