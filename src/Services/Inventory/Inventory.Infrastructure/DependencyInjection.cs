using Inventory.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Inventory.Infrastructure;

public sealed class InventoryDatabaseOptions
{
    public string Database { get; set; } = "";
}

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InventoryDatabaseOptions>()
            .Configure(options => options.Database = configuration.GetConnectionString("Database") ?? "")
            .Validate(options => IsInventoryConnection(options.Database),
                "ConnectionStrings:Database must target inventory_db as inventory_app. Supply it through environment configuration.")
            .ValidateOnStart();
        services.AddSingleton(provider => NpgsqlDataSource.Create(
            provider.GetRequiredService<IOptions<InventoryDatabaseOptions>>().Value.Database));
        services.AddDbContext<InventoryDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));
        services.AddScoped<IInventoryReader, DapperInventoryReader>();
        services.AddScoped<IInventoryWriter, EfInventoryWriter>();
        services.AddScoped<InventoryService>();
        services.AddScoped<InventorySeedData>();
        return services;
    }

    private static bool IsInventoryConnection(string value)
    {
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(value);
            return connection.Database == "inventory_db" && connection.Username == "inventory_app"
                && !string.IsNullOrWhiteSpace(connection.Host);
        }
        catch (ArgumentException) { return false; }
    }
}
