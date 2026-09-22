namespace Catalog.Application;

public sealed record CategoryRequest(string? Name, string? Description);
public sealed record ProductRequest(string? Name, string? Description, decimal? UnitPrice, Guid? CategoryId);
public sealed record CategoryDto(Guid Id, string Name, string Description);
public sealed record ProductDto(Guid Id, string Name, string Description, decimal UnitPrice, Guid CategoryId, string CategoryName)
{
    public string Currency => "USD";
}
public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize);
public sealed record CatalogSearch(int Page = 1, int PageSize = 20, string? Search = null, Guid? CategoryId = null);
