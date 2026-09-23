using Ordering.Application;

namespace Ordering.Api;

public static class OrderingEndpoints
{
    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var orders = endpoints.MapGroup("/api/orders").WithTags("Orders");
        orders.MapGet("/", async (Guid customerId, OrderingService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            TypedResults.Ok(await service.ListForCustomerAsync(customerId, page, pageSize, ct))).ProducesValidationProblem();
        orders.MapGet("/{id:guid}", async (Guid id, OrderingService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetAsync(id, ct))).ProducesValidationProblem().ProducesProblem(404);
        // No HTTP writes until Phase 6 resolves authoritative prices and availability server-side.
        return endpoints;
    }
}
