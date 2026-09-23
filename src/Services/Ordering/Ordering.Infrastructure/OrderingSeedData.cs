using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ordering.Domain;

namespace Ordering.Infrastructure;

public sealed class OrderingSeedData(OrderingDbContext db, ILogger<OrderingSeedData> logger)
{
    // Synthetic learning data, not authenticated identities or live product/stock lookups.
    public static readonly Guid CustomerId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    public static readonly Guid PendingOrderId = Guid.Parse("44444444-4444-4444-4444-444444444441");
    public static readonly Guid CancelledOrderId = Guid.Parse("44444444-4444-4444-4444-444444444442");

    public async Task SeedAsync(CancellationToken ct)
    {
        var pending = new Order(PendingOrderId, CustomerId, new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc),
        [
            new OrderItem(Guid.Parse("22222222-2222-2222-2222-222222222221"), "Laptop", 999.99m, 1),
            new OrderItem(Guid.Parse("22222222-2222-2222-2222-222222222223"), "Mouse", 29.99m, 2)
        ]);
        var cancelled = new Order(CancelledOrderId, CustomerId, new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc),
            [new OrderItem(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Keyboard", 79.99m, 1)]);
        cancelled.TransitionTo(OrderStatus.Cancelled, "Development history example");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var order in new[] { pending, cancelled })
            if (!await db.Orders.AnyAsync(x => x.Id == order.Id, ct)) db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Ordering development seed completed without overwriting existing orders");
    }
}
