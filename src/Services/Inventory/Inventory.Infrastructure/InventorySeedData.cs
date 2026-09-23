using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

public sealed class InventorySeedData(InventoryDbContext db, ILogger<InventorySeedData> logger)
{
    // Shared business IDs by documented seed convention, never a Catalog project/database dependency.
    public static readonly Guid LaptopId = Guid.Parse("22222222-2222-2222-2222-222222222221");

    public async Task SeedAsync(CancellationToken ct)
    {
        var items = new[]
        {
            new InventoryItem(LaptopId, 10),
            new InventoryItem(Guid.Parse("22222222-2222-2222-2222-222222222222"), 25),
            new InventoryItem(Guid.Parse("22222222-2222-2222-2222-222222222223"), 40),
            new InventoryItem(Guid.Parse("22222222-2222-2222-2222-222222222224"), 15),
            new InventoryItem(Guid.Parse("22222222-2222-2222-2222-222222222225"), 20)
        };
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var item in items)
            if (!await db.Items.AnyAsync(x => x.ProductId == item.ProductId, ct)) db.Items.Add(item);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Inventory development seed completed without overwriting existing stock");
    }
}
