using Ordering.Application;
using Ordering.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        orders.MapPost("/", async ([FromBody] CheckoutRequest? request, CheckoutService service,
            IOptions<ServiceCommunicationOptions> options, CancellationToken ct) =>
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(options.Value.CheckoutTimeoutMilliseconds);
            try
            {
                var order = await service.CreateAsync(request, deadline.Token);
                return TypedResults.Created($"/api/orders/{order.Id}", order);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new OrderingDependencyException("Checkout", DependencyFailure.Timeout);
            }
        }).ProducesValidationProblem().ProducesProblem(409).ProducesProblem(502).ProducesProblem(503).ProducesProblem(504);
        return endpoints;
    }
}
