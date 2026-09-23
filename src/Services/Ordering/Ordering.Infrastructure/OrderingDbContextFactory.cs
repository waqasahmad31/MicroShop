using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ordering.Infrastructure;

public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__Database with scripts/Set-ServiceEnvironment.ps1 -Service Ordering first.");
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (parsed.Database != "ordering_db" || parsed.Username != "ordering_app")
            throw new InvalidOperationException("Ordering migrations require ordering_db and ordering_app.");
        return new OrderingDbContext(new DbContextOptionsBuilder<OrderingDbContext>().UseNpgsql(connection).Options);
    }
}
