using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ordering.Application;
using Ordering.Domain;

namespace Ordering.Infrastructure;

public sealed class EfOrderingWriter(OrderingDbContext db, ILogger<EfOrderingWriter> logger) : IOrderingWriter
{
    public async Task CreateAsync(Order order, CancellationToken ct)
    {
        db.Orders.Add(order);
        try { await db.SaveChangesAsync(ct); } // One SaveChanges transaction includes header and all lines.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "pk_orders" })
        { throw new OrderingConflictException("An order with this ID already exists."); }
        logger.LogInformation("Created pending order {OrderId} for customer {CustomerId}", order.Id, order.CustomerId);
    }

    public async Task<bool> TransitionAsync(Guid id, OrderStatus target, string? reason, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var rows = await db.Orders.FromSql($"SELECT * FROM orders WHERE id = {id} FOR UPDATE").ToListAsync(ct);
        var order = rows.SingleOrDefault();
        if (order is null) return false;
        // Refresh an already tracked aggregate as well: a scoped caller may have read it earlier.
        await db.Entry(order).ReloadAsync(ct);
        order.TransitionTo(target, reason);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Order {OrderId} is now {Status}", id, order.Status);
        return true;
    }
}
