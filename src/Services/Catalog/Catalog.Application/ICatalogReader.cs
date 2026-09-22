namespace Catalog.Application;

public interface ICatalogReader
{
    Task<ProductDto?> GetProductAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryDto?> GetCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<ProductDto>> ListProductsAsync(CatalogSearch query, CancellationToken cancellationToken);
    Task<PagedResult<CategoryDto>> ListCategoriesAsync(CatalogSearch query, CancellationToken cancellationToken);
}
