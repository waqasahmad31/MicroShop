using System.Data;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Inventory.Infrastructure;

public sealed class EfInventoryWriter(InventoryDbContext db, ILogger<EfInventoryWriter> logger) : IInventoryWriter
{
    public async Task CreateAsync(InventoryItem item, CancellationToken ct)
    {
        db.Items.Add(item);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "pk_inventory_items" })
        {
            throw new InventoryConflictException("Inventory already exists for this product.");
        }
        logger.LogInformation("Created inventory for product {ProductId} with {OnHand} units", item.ProductId, item.OnHand);
    }

    public async Task<InventoryItemDto?> AdjustAsync(Guid productId, int delta, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        // Interpolation in FromSql produces parameters. The lock is held through SaveChanges and commit.
        // A concurrent writer waits and then reads the latest committed quantities.
        var items = await db.Items.FromSql(
            $"SELECT product_id, on_hand, reserved FROM inventory_items WHERE product_id = {productId} FOR UPDATE")
            .ToListAsync(ct);
        var item = items.SingleOrDefault();
        if (item is null) return null;
        item.AdjustOnHand(delta);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Adjusted product {ProductId} by {Delta}; on-hand {OnHand}, reserved {Reserved}",
            productId, delta, item.OnHand, item.Reserved);
        return new(item.ProductId, item.OnHand, item.Reserved, item.Available);
    }
}
