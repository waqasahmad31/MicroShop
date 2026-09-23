using Ordering.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Ordering.Infrastructure;

public sealed class OrderingDatabaseOptions
{
    public string Database { get; set; } = "";
}

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OrderingDatabaseOptions>()
            .Configure(options => options.Database = configuration.GetConnectionString("Database") ?? "")
            .Validate(options => IsOrderingConnection(options.Database),
                "ConnectionStrings:Database must target ordering_db as ordering_app. Supply it through environment configuration.")
            .ValidateOnStart();
        services.AddSingleton(provider => NpgsqlDataSource.Create(
            provider.GetRequiredService<IOptions<OrderingDatabaseOptions>>().Value.Database));
        services.AddDbContext<OrderingDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));
        services.AddScoped<IOrderingReader, DapperOrderingReader>();
        services.AddScoped<IOrderingWriter, EfOrderingWriter>();
        services.AddScoped<OrderingService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<OrderingSeedData>();
        return services;
    }

    private static bool IsOrderingConnection(string value)
    {
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(value);
            return connection.Database == "ordering_db" && connection.Username == "ordering_app"
                && !string.IsNullOrWhiteSpace(connection.Host);
        }
        catch (ArgumentException) { return false; }
    }
}
