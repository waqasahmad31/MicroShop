namespace Ordering.Application;

public interface IOrderingReader
{
    Task<OrderDetailsDto?> GetAsync(Guid id, CancellationToken ct);
    Task<OrderPage> ListForCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken ct);
}
