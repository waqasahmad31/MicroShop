using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__Database with scripts/Set-ServiceEnvironment.ps1 -Service Inventory first.");
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (parsed.Database != "inventory_db" || parsed.Username != "inventory_app")
            throw new InvalidOperationException("Inventory migrations require inventory_db and inventory_app.");
        return new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connection).Options);
    }
}
