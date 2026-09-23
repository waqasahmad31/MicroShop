namespace Ordering.Application;

// Priced lines are trusted in-process inputs, never the public checkout request contract.
public sealed record PricedOrderLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
public sealed record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
public sealed record OrderSummaryDto(Guid Id, Guid CustomerId, DateTime CreatedAtUtc, string Status, string? StatusReason, decimal Total)
{
    public string Currency => "USD";
}
public sealed record OrderDetailsDto(Guid Id, Guid CustomerId, DateTime CreatedAtUtc, string Status,
    string? StatusReason, decimal Total, IReadOnlyList<OrderItemDto> Items)
{
    public string Currency => "USD";
}
public sealed record OrderPage(IReadOnlyList<OrderSummaryDto> Items, long TotalCount, int Page, int PageSize);
