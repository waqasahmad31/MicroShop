using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__Database with scripts/Set-ServiceEnvironment.ps1 -Service Catalog first.");
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (parsed.Database != "catalog_db" || parsed.Username != "catalog_app")
            throw new InvalidOperationException("Catalog migrations require catalog_db and catalog_app.");
        return new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connection).Options);
    }
}
