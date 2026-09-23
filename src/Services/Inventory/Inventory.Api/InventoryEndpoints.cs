using Inventory.Application;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var items = endpoints.MapGroup("/api/inventory/items").WithTags("Inventory");
        items.MapGet("/", async (InventoryService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            TypedResults.Ok(await service.ListAsync(page, pageSize, ct))).ProducesValidationProblem();
        items.MapGet("/{productId:guid}", async (Guid productId, InventoryService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetAsync(productId, ct))).ProducesValidationProblem().ProducesProblem(404);
        items.MapPost("/", async ([FromBody] CreateInventoryItemRequest? request, InventoryService service, CancellationToken ct) =>
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/inventory/items/{item.ProductId}", item);
        }).ProducesValidationProblem().ProducesProblem(409);
        items.MapPost("/{productId:guid}/adjustments",
            async (Guid productId, [FromBody] AdjustStockRequest? request, InventoryService service, CancellationToken ct) =>
                TypedResults.Ok(await service.AdjustAsync(productId, request, ct)))
            .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409);
        return endpoints;
    }
}
