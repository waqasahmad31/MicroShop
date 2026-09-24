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
        services.AddOptions<ServiceCommunicationOptions>().Bind(configuration.GetSection("Services"))
            .Validate(options => options.Catalog is not null && options.Catalog.IsValid()
                && options.Inventory is not null && options.Inventory.IsValid()
                && options.CheckoutTimeoutMilliseconds is >= 1000 and <= 60000,
                "Services requires HTTP(S) origin URLs, 100–30000 ms dependency timeouts and a 1000–60000 ms checkout timeout.")
            .ValidateOnStart();
        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>((provider, client) =>
            ConfigureClient(client, provider.GetRequiredService<IOptions<ServiceCommunicationOptions>>().Value.Catalog))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>((provider, client) =>
            ConfigureClient(client, provider.GetRequiredService<IOptions<ServiceCommunicationOptions>>().Value.Inventory))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false });
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
        services.AddScoped<CheckoutService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<OrderingSeedData>();
        return services;
    }

    private static void ConfigureClient(HttpClient client, ServiceEndpointOptions options)
    {
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMilliseconds);
        client.MaxResponseContentBufferSize = 64 * 1024;
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
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
