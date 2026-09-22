using Catalog.Domain;

namespace Catalog.Application;

// This command-specific persistence boundary keeps EF out of Application.
// It exposes atomic writes, not IQueryable, DbSet or a generic repository.
public interface ICatalogWriter
{
    Task CreateProductAsync(Product product, CancellationToken cancellationToken);
    Task<bool> UpdateProductAsync(Product product, CancellationToken cancellationToken);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken);
    Task CreateCategoryAsync(Category category, CancellationToken cancellationToken);
    Task<bool> UpdateCategoryAsync(Category category, CancellationToken cancellationToken);
    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);
}
