using Dapper;
using Inventory.Application;
using Npgsql;

namespace Inventory.Infrastructure;

public sealed class DapperInventoryReader(NpgsqlDataSource dataSource) : IInventoryReader
{
    private const string Columns = """
        product_id AS "ProductId", on_hand AS "OnHand", reserved AS "Reserved",
        on_hand - reserved AS "Available"
        """;

    public async Task<InventoryItemDto?> GetAsync(Guid productId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<InventoryItemDto>(new CommandDefinition(
            $"SELECT {Columns} FROM inventory_items WHERE product_id = @ProductId",
            new { ProductId = productId }, cancellationToken: ct));
    }

    public async Task<InventoryPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(
            $"""
            SELECT count(*) FROM inventory_items;
            SELECT {Columns} FROM inventory_items ORDER BY product_id LIMIT @PageSize OFFSET @Offset;
            """,
            new { PageSize = pageSize, Offset = (long)(page - 1) * pageSize }, cancellationToken: ct));
        var count = await result.ReadSingleAsync<long>();
        var items = (await result.ReadAsync<InventoryItemDto>()).AsList();
        return new(items, count, page, pageSize);
    }
}
