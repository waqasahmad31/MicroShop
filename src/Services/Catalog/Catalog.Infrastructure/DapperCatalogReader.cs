using Catalog.Application;
using Dapper;
using Npgsql;

namespace Catalog.Infrastructure;

public sealed class DapperCatalogReader(NpgsqlDataSource dataSource) : ICatalogReader
{
    private const string ProductColumns = """
        p.id AS "Id", p.name AS "Name", p.description AS "Description",
        p.unit_price AS "UnitPrice", p.category_id AS "CategoryId", c.name AS "CategoryName"
        """;
    private const string CategoryColumns = """id AS "Id", name AS "Name", description AS "Description" """;

    public async Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ProductDto>(new CommandDefinition(
            $"SELECT {ProductColumns} FROM products p JOIN categories c ON c.id = p.category_id WHERE p.id = @Id",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task<CategoryDto?> GetCategoryAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<CategoryDto>(new CommandDefinition(
            $"SELECT {CategoryColumns} FROM categories WHERE id = @Id", new { Id = id }, cancellationToken: ct));
    }

    public async Task<PagedResult<ProductDto>> ListProductsAsync(CatalogSearch query, CancellationToken ct)
    {
        const string filter = """
            WHERE (@Pattern IS NULL OR p.name ILIKE @Pattern OR p.description ILIKE @Pattern)
              AND (@CategoryId IS NULL OR p.category_id = @CategoryId)
            """;
        // Only fixed SQL fragments are interpolated. All user input is passed as parameters.
        var sql = $"""
            SELECT count(*) FROM products p {filter};
            SELECT {ProductColumns} FROM products p JOIN categories c ON c.id = p.category_id
            {filter} ORDER BY p.name, p.id LIMIT @PageSize OFFSET @Offset;
            """;
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(sql,
            new { Pattern = Pattern(query.Search), query.CategoryId, query.PageSize, Offset = (long)(query.Page - 1) * query.PageSize },
            cancellationToken: ct));
        var count = await result.ReadSingleAsync<long>();
        var items = (await result.ReadAsync<ProductDto>()).AsList();
        return new(items, count, query.Page, query.PageSize);
    }

    public async Task<PagedResult<CategoryDto>> ListCategoriesAsync(CatalogSearch query, CancellationToken ct)
    {
        const string filter = "WHERE (@Pattern IS NULL OR name ILIKE @Pattern OR description ILIKE @Pattern)";
        var sql = $"""
            SELECT count(*) FROM categories {filter};
            SELECT {CategoryColumns} FROM categories {filter} ORDER BY name, id LIMIT @PageSize OFFSET @Offset;
            """;
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(sql,
            new { Pattern = Pattern(query.Search), query.PageSize, Offset = (long)(query.Page - 1) * query.PageSize },
            cancellationToken: ct));
        var count = await result.ReadSingleAsync<long>();
        var items = (await result.ReadAsync<CategoryDto>()).AsList();
        return new(items, count, query.Page, query.PageSize);
    }

    private static string? Pattern(string? search) => search is null ? null :
        "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
