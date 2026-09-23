using Ordering.Domain;

namespace Ordering.Application;

public sealed class OrderingService(IOrderingReader reader, IOrderingWriter writer, TimeProvider clock)
{
    // Internal foundation only. Phase 6 obtains authoritative snapshots before invoking this operation.
    public async Task<OrderDetailsDto> CreateFromPricedLinesAsync(Guid customerId, IReadOnlyList<PricedOrderLine>? lines, CancellationToken ct)
    {
        if (lines is null || lines.Count is < 1 or > Order.MaximumLines || lines.Any(x => x is null))
            throw new OrderingValidationException("Items", "An order must contain 1–100 non-null input lines.");
        var order = new Order(Guid.NewGuid(), customerId, clock.GetUtcNow().UtcDateTime,
            lines.Select(x => new OrderItem(x.ProductId, x.ProductName, x.UnitPrice, x.Quantity)).ToArray());
        await writer.CreateAsync(order, ct);
        return new(order.Id, order.CustomerId, order.CreatedAtUtc, order.Status.ToString(), order.StatusReason, order.Total,
            order.Items.Select(x => new OrderItemDto(x.ProductId, x.ProductName, x.UnitPrice, x.Quantity, x.LineTotal)).ToArray());
    }

    public async Task TransitionAsync(Guid id, OrderStatus target, string? reason, CancellationToken ct)
    {
        ValidateId(id, "Id");
        if (!await writer.TransitionAsync(id, target, reason, ct)) throw new OrderingNotFoundException(id);
    }

    public async Task<OrderDetailsDto> GetAsync(Guid id, CancellationToken ct)
    {
        ValidateId(id, "Id");
        return await reader.GetAsync(id, ct) ?? throw new OrderingNotFoundException(id);
    }

    public Task<OrderPage> ListForCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken ct)
    {
        ValidateId(customerId, "CustomerId");
        if (page < 1) throw new OrderingValidationException("Page", "Page must be at least 1.");
        if (pageSize is < 1 or > 100) throw new OrderingValidationException("PageSize", "Page size must be between 1 and 100.");
        return reader.ListForCustomerAsync(customerId, page, pageSize, ct);
    }

    private static void ValidateId(Guid id, string field)
    {
        if (id == Guid.Empty) throw new OrderingValidationException(field, "ID cannot be empty.");
    }
}
