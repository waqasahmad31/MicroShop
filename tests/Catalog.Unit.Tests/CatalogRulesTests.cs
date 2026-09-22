using Catalog.Application;
using Catalog.Domain;
using Xunit;

namespace Catalog.Unit.Tests;

public sealed class CatalogRulesTests
{
    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.001")]
    [InlineData("100000000")]
    public void Invalid_price_does_not_partially_modify_product(string value)
    {
        var categoryId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "Original", "Description", 10m, categoryId);
        Assert.Throws<CatalogValidationException>(() => product.Update("Changed", "Changed",
            decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), categoryId));
        Assert.Equal("Original", product.Name);
        Assert.Equal(10m, product.UnitPrice);
    }

    [Fact]
    public void Names_are_trimmed_and_category_uniqueness_key_is_normalized()
    {
        var category = new Category(Guid.NewGuid(), "  Accessories  ", null);
        Assert.Equal("Accessories", category.Name);
        Assert.Equal("ACCESSORIES", category.NormalizedName);
        Assert.Equal("", category.Description);
        Assert.Throws<CatalogValidationException>(() => new Category(Guid.NewGuid(), " ", null));
        Assert.Throws<CatalogValidationException>(() => new Product(Guid.NewGuid(), "Mouse", null, 0, Guid.Empty));
    }

    [Fact]
    public async Task Missing_category_prevents_write_and_passes_cancellation_token()
    {
        var persistence = new FakePersistence();
        var service = new CatalogService(persistence, persistence);
        using var cancellation = new CancellationTokenSource();
        var error = await Assert.ThrowsAsync<CatalogValidationException>(() => service.CreateProductAsync(
            new("Mouse", null, 10, Guid.NewGuid()), cancellation.Token));
        Assert.Equal("CategoryId", error.Field);
        Assert.Equal(cancellation.Token, persistence.LastToken);
        Assert.Equal(0, persistence.Writes);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Invalid_pagination_is_rejected_before_read(int page, int pageSize)
    {
        var persistence = new FakePersistence();
        var service = new CatalogService(persistence, persistence);
        await Assert.ThrowsAsync<CatalogValidationException>(() => service.ListProductsAsync(new(page, pageSize), default));
        Assert.Equal(0, persistence.ListReads);
    }

    [Fact]
    public async Task Missing_product_maps_to_application_not_found()
    {
        var persistence = new FakePersistence();
        await Assert.ThrowsAsync<CatalogNotFoundException>(() => new CatalogService(persistence, persistence)
            .GetProductAsync(Guid.NewGuid(), default));
    }

    private sealed class FakePersistence : ICatalogReader, ICatalogWriter
    {
        public int Writes { get; private set; }
        public int ListReads { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public Task<CategoryDto?> GetCategoryAsync(Guid id, CancellationToken ct)
        { LastToken = ct; return Task.FromResult<CategoryDto?>(null); }
        public Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct) => Task.FromResult<ProductDto?>(null);
        public Task<PagedResult<ProductDto>> ListProductsAsync(CatalogSearch query, CancellationToken ct)
        { ListReads++; return Task.FromResult(new PagedResult<ProductDto>([], 0, query.Page, query.PageSize)); }
        public Task<PagedResult<CategoryDto>> ListCategoriesAsync(CatalogSearch query, CancellationToken ct) => throw new NotSupportedException();
        public Task CreateProductAsync(Product product, CancellationToken ct) { Writes++; return Task.CompletedTask; }
        public Task CreateCategoryAsync(Category category, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> UpdateProductAsync(Product product, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteProductAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> UpdateCategoryAsync(Category category, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
}
