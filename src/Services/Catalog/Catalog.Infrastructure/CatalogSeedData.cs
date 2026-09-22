using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure;

public sealed class CatalogSeedData(CatalogDbContext db, ILogger<CatalogSeedData> logger)
{
    public static readonly Guid ComputersId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AccessoriesId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    public static readonly Guid LaptopId = Guid.Parse("22222222-2222-2222-2222-222222222221");

    public async Task SeedAsync(CancellationToken ct)
    {
        // Stable IDs let later Inventory seed its own records without querying catalog_db.
        // Existing rows are never overwritten; this is an explicit development command.
        var categories = new[]
        {
            new Category(ComputersId, "Computers", "Computers and displays"),
            new Category(AccessoriesId, "Accessories", "Everyday computer accessories")
        };
        var products = new[]
        {
            new Product(LaptopId, "Laptop", "Everyday development laptop", 999.99m, ComputersId),
            new Product(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Keyboard", "Mechanical keyboard", 79.99m, AccessoriesId),
            new Product(Guid.Parse("22222222-2222-2222-2222-222222222223"), "Mouse", "Wireless mouse", 29.99m, AccessoriesId),
            new Product(Guid.Parse("22222222-2222-2222-2222-222222222224"), "Monitor", "27-inch monitor", 249.99m, ComputersId),
            new Product(Guid.Parse("22222222-2222-2222-2222-222222222225"), "Headphones", "Over-ear headphones", 59.99m, AccessoriesId)
        };

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var category in categories)
            if (!await db.Categories.AnyAsync(x => x.Id == category.Id, ct)) db.Categories.Add(category);
        foreach (var product in products)
            if (!await db.Products.AnyAsync(x => x.Id == product.Id, ct)) db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Catalog development seed completed without overwriting existing rows");
    }
}
