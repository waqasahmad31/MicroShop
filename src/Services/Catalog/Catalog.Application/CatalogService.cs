using Catalog.Domain;

namespace Catalog.Application;

public sealed class CatalogService(ICatalogReader reader, ICatalogWriter writer)
{
    public async Task<ProductDto> CreateProductAsync(ProductRequest? request, CancellationToken ct)
    {
        var (product, category) = await PrepareProductAsync(Guid.NewGuid(), request, ct);
        await writer.CreateProductAsync(product, ct);
        return ToDto(product, category.Name);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, ProductRequest? request, CancellationToken ct)
    {
        var (product, category) = await PrepareProductAsync(id, request, ct);
        if (!await writer.UpdateProductAsync(product, ct)) throw new CatalogNotFoundException("Product", id);
        return ToDto(product, category.Name);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct)
    {
        if (!await writer.DeleteProductAsync(id, ct)) throw new CatalogNotFoundException("Product", id);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryRequest? request, CancellationToken ct)
    {
        RequireBody(request);
        var category = new Category(Guid.NewGuid(), request!.Name, request.Description);
        await writer.CreateCategoryAsync(category, ct);
        return new(category.Id, category.Name, category.Description);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, CategoryRequest? request, CancellationToken ct)
    {
        RequireId(id);
        RequireBody(request);
        var category = new Category(id, request!.Name, request.Description);
        if (!await writer.UpdateCategoryAsync(category, ct)) throw new CatalogNotFoundException("Category", id);
        return new(category.Id, category.Name, category.Description);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct)
    {
        if (!await writer.DeleteCategoryAsync(id, ct)) throw new CatalogNotFoundException("Category", id);
    }

    public async Task<ProductDto> GetProductAsync(Guid id, CancellationToken ct) =>
        await reader.GetProductAsync(id, ct) ?? throw new CatalogNotFoundException("Product", id);

    public async Task<CategoryDto> GetCategoryAsync(Guid id, CancellationToken ct) =>
        await reader.GetCategoryAsync(id, ct) ?? throw new CatalogNotFoundException("Category", id);

    public Task<PagedResult<ProductDto>> ListProductsAsync(CatalogSearch query, CancellationToken ct) =>
        reader.ListProductsAsync(ValidateSearch(query), ct);

    public Task<PagedResult<CategoryDto>> ListCategoriesAsync(CatalogSearch query, CancellationToken ct) =>
        reader.ListCategoriesAsync(ValidateSearch(query), ct);

    private async Task<(Product Product, CategoryDto Category)> PrepareProductAsync(Guid id, ProductRequest? request, CancellationToken ct)
    {
        RequireId(id);
        RequireBody(request);
        if (request!.UnitPrice is null) throw new CatalogValidationException("UnitPrice", "Price is required.");
        var product = new Product(id, request.Name, request.Description, request.UnitPrice.Value, request.CategoryId ?? Guid.Empty);
        var category = await reader.GetCategoryAsync(product.CategoryId, ct)
            ?? throw new CatalogValidationException("CategoryId", "The selected category does not exist.");
        return (product, category);
    }

    private static CatalogSearch ValidateSearch(CatalogSearch query)
    {
        if (query.Page < 1) throw new CatalogValidationException("Page", "Page must be at least 1.");
        if (query.PageSize is < 1 or > 100) throw new CatalogValidationException("PageSize", "Page size must be between 1 and 100.");
        var search = query.Search?.Trim();
        if (search?.Length > 120) throw new CatalogValidationException("Search", "Search cannot exceed 120 characters.");
        if (query.CategoryId == Guid.Empty) throw new CatalogValidationException("CategoryId", "Category ID cannot be empty.");
        return query with { Search = string.IsNullOrEmpty(search) ? null : search };
    }

    private static void RequireId(Guid id)
    {
        if (id == Guid.Empty) throw new CatalogValidationException("Id", "ID cannot be empty.");
    }

    private static void RequireBody(object? request)
    {
        if (request is null) throw new CatalogValidationException("Body", "A JSON request body is required.");
    }

    private static ProductDto ToDto(Product product, string categoryName) =>
        new(product.Id, product.Name, product.Description, product.UnitPrice, product.CategoryId, categoryName);
}
