using Catalog.Application;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var products = endpoints.MapGroup("/api/catalog/products").WithTags("Products");
        products.MapGet("/", async (CatalogService service, CancellationToken ct,
            int page = 1, int pageSize = 20, string? search = null, Guid? categoryId = null) =>
            TypedResults.Ok(await service.ListProductsAsync(new(page, pageSize, search, categoryId), ct)))
            .ProducesValidationProblem();
        products.MapGet("/{id:guid}", async (Guid id, CatalogService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetProductAsync(id, ct))).ProducesProblem(404);
        products.MapPost("/", async ([FromBody] ProductRequest? request, CatalogService service, CancellationToken ct) =>
        {
            var product = await service.CreateProductAsync(request, ct);
            return TypedResults.Created($"/api/catalog/products/{product.Id}", product);
        }).ProducesValidationProblem().ProducesProblem(409);
        products.MapPut("/{id:guid}", async (Guid id, [FromBody] ProductRequest? request, CatalogService service, CancellationToken ct) =>
            TypedResults.Ok(await service.UpdateProductAsync(id, request, ct)))
            .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409);
        products.MapDelete("/{id:guid}", async (Guid id, CatalogService service, CancellationToken ct) =>
        {
            await service.DeleteProductAsync(id, ct);
            return TypedResults.NoContent();
        }).ProducesProblem(404);

        var categories = endpoints.MapGroup("/api/catalog/categories").WithTags("Categories");
        categories.MapGet("/", async (CatalogService service, CancellationToken ct,
            int page = 1, int pageSize = 20, string? search = null) =>
            TypedResults.Ok(await service.ListCategoriesAsync(new(page, pageSize, search), ct)))
            .ProducesValidationProblem();
        categories.MapGet("/{id:guid}", async (Guid id, CatalogService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetCategoryAsync(id, ct))).ProducesProblem(404);
        categories.MapPost("/", async ([FromBody] CategoryRequest? request, CatalogService service, CancellationToken ct) =>
        {
            var category = await service.CreateCategoryAsync(request, ct);
            return TypedResults.Created($"/api/catalog/categories/{category.Id}", category);
        }).ProducesValidationProblem().ProducesProblem(409);
        categories.MapPut("/{id:guid}", async (Guid id, [FromBody] CategoryRequest? request, CatalogService service, CancellationToken ct) =>
            TypedResults.Ok(await service.UpdateCategoryAsync(id, request, ct)))
            .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409);
        categories.MapDelete("/{id:guid}", async (Guid id, CatalogService service, CancellationToken ct) =>
        {
            await service.DeleteCategoryAsync(id, ct);
            return TypedResults.NoContent();
        }).ProducesProblem(404).ProducesProblem(409);

        return endpoints;
    }
}
