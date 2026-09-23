using Dapper;
using Npgsql;
using Ordering.Application;

namespace Ordering.Infrastructure;

public sealed class DapperOrderingReader(NpgsqlDataSource dataSource) : IOrderingReader
{
    private const string SummaryColumns = """
        o.id AS "Id", o.customer_id AS "CustomerId", o.created_at_utc AS "CreatedAtUtc",
        o.status AS "Status", o.status_reason AS "StatusReason",
        (SELECT COALESCE(sum(i.unit_price * i.quantity), 0) FROM order_items i WHERE i.order_id = o.id) AS "Total"
        """;

    public async Task<OrderDetailsDto?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition($"""
            SELECT {SummaryColumns} FROM orders o WHERE o.id = @Id;
            SELECT product_id AS "ProductId", product_name AS "ProductName", unit_price AS "UnitPrice",
                quantity AS "Quantity", unit_price * quantity AS "LineTotal"
            FROM order_items WHERE order_id = @Id ORDER BY product_id;
            """, new { Id = id }, cancellationToken: ct));
        var order = await result.ReadSingleOrDefaultAsync<OrderSummaryDto>();
        if (order is null) return null;
        var items = (await result.ReadAsync<OrderItemDto>()).AsList();
        return new(order.Id, order.CustomerId, order.CreatedAtUtc, order.Status, order.StatusReason, order.Total, items);
    }

    public async Task<OrderPage> ListForCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition($"""
            SELECT count(*) FROM orders WHERE customer_id = @CustomerId;
            SELECT {SummaryColumns} FROM orders o WHERE o.customer_id = @CustomerId
            ORDER BY o.created_at_utc DESC, o.id DESC LIMIT @PageSize OFFSET @Offset;
            """, new { CustomerId = customerId, PageSize = pageSize, Offset = (long)(page - 1) * pageSize }, cancellationToken: ct));
        var count = await result.ReadSingleAsync<long>();
        var items = (await result.ReadAsync<OrderSummaryDto>()).AsList();
        return new(items, count, page, pageSize);
    }
}
