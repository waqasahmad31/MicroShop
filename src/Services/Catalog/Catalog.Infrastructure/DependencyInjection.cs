using Catalog.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Catalog.Infrastructure;

public sealed class CatalogDatabaseOptions
{
    public string Database { get; set; } = "";
}

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CatalogDatabaseOptions>()
            .Configure(options => options.Database = configuration.GetConnectionString("Database") ?? "")
            .Validate(options => IsCatalogConnection(options.Database),
                "ConnectionStrings:Database must target catalog_db as catalog_app. Supply it through environment configuration.")
            .ValidateOnStart();
        services.AddSingleton(provider => NpgsqlDataSource.Create(
            provider.GetRequiredService<IOptions<CatalogDatabaseOptions>>().Value.Database));
        services.AddDbContext<CatalogDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));
        services.AddScoped<ICatalogReader, DapperCatalogReader>();
        services.AddScoped<ICatalogWriter, EfCatalogWriter>();
        services.AddScoped<CatalogService>();
        services.AddScoped<CatalogSeedData>();
        return services;
    }

    private static bool IsCatalogConnection(string value)
    {
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(value);
            return connection.Database == "catalog_db" && connection.Username == "catalog_app"
                && !string.IsNullOrWhiteSpace(connection.Host);
        }
        catch (ArgumentException) { return false; }
    }
}
