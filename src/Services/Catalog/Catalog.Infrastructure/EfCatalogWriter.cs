using Catalog.Application;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Catalog.Infrastructure;

public sealed class EfCatalogWriter(CatalogDbContext db, ILogger<EfCatalogWriter> logger) : ICatalogWriter
{
    public async Task CreateProductAsync(Product product, CancellationToken ct)
    {
        db.Products.Add(product);
        await SaveAsync(ct);
        logger.LogInformation("Created product {ProductId}", product.Id);
    }

    public async Task<bool> UpdateProductAsync(Product product, CancellationToken ct)
    {
        db.Entry(product).State = EntityState.Modified;
        var saved = await SaveAsync(ct);
        if (saved) logger.LogInformation("Updated product {ProductId}", product.Id);
        return saved;
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return false;
        db.Products.Remove(product);
        var saved = await SaveAsync(ct);
        if (saved) logger.LogInformation("Deleted product {ProductId}", id);
        return saved;
    }

    public async Task CreateCategoryAsync(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        await SaveAsync(ct);
        logger.LogInformation("Created category {CategoryId}", category.Id);
    }

    public async Task<bool> UpdateCategoryAsync(Category category, CancellationToken ct)
    {
        db.Entry(category).State = EntityState.Modified;
        var saved = await SaveAsync(ct);
        if (saved) logger.LogInformation("Updated category {CategoryId}", category.Id);
        return saved;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return false;
        db.Categories.Remove(category);
        var saved = await SaveAsync(ct);
        if (saved) logger.LogInformation("Deleted category {CategoryId}", id);
        return saved;
    }

    private async Task<bool> SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // A full PUT uses last-write-wins; zero affected rows means it was removed/not found.
            return false;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new CatalogConflictException("A category with this name already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new CatalogConflictException("Products must reference an existing category. Delete or move its products before deleting a category.");
        }
    }
}
