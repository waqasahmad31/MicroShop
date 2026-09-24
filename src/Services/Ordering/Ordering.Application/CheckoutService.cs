using Ordering.Domain;

namespace Ordering.Application;

public sealed record CheckoutItem(Guid? ProductId, int? Quantity);
public sealed record CheckoutRequest(Guid? CustomerId, IReadOnlyList<CheckoutItem?>? Items);

public sealed class CheckoutService(ICatalogServiceClient catalog, IInventoryServiceClient inventory, OrderingService orders)
{
    public async Task<OrderDetailsDto> CreateAsync(CheckoutRequest? request, CancellationToken ct)
    {
        if (request?.CustomerId is null || request.CustomerId == Guid.Empty)
            throw new OrderingValidationException("CustomerId", "Customer ID is required and cannot be empty.");
        if (request.Items is null || request.Items.Count is < 1 or > Order.MaximumLines)
            throw new OrderingValidationException("Items", "An order must contain 1–100 input lines.");

        // Validate every input and merge duplicates before making any network calls.
        var quantities = new Dictionary<Guid, int>();
        foreach (var item in request.Items)
        {
            if (item?.ProductId is null || item.ProductId == Guid.Empty)
                throw new OrderingValidationException("ProductId", "Product ID is required and cannot be empty.");
            if (item.Quantity is null or < 1 or > OrderItem.MaximumQuantity)
                throw new OrderingValidationException("Quantity", "Quantity must be between 1 and 1000.");
            var quantity = quantities.GetValueOrDefault(item.ProductId.Value) + item.Quantity.Value;
            if (quantity > OrderItem.MaximumQuantity)
                throw new OrderingValidationException("Quantity", "Combined quantity per product cannot exceed 1000.");
            quantities[item.ProductId.Value] = quantity;
        }

        var lines = new List<PricedOrderLine>();
        foreach (var (productId, quantity) in quantities)
        {
            ct.ThrowIfCancellationRequested();
            var product = await catalog.GetProductAsync(productId, ct)
                ?? throw new OrderingConflictException($"Product '{productId}' is no longer available in Catalog.");
            var stock = await inventory.GetAvailabilityAsync(productId, ct);
            if (stock is null || stock.Available < quantity)
                throw new OrderingConflictException($"Product '{productId}' has insufficient available stock.");
            lines.Add(new(productId, product.Name, product.UnitPrice, quantity));
        }

        // No write or database transaction exists during the remote checks. Availability is not a reservation.
        ct.ThrowIfCancellationRequested();
        return await orders.CreateFromPricedLinesAsync(request.CustomerId.Value, lines, ct);
    }
}
