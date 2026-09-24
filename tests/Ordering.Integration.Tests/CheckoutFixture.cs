extern alias CatalogApi;
extern alias InventoryApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ordering.Infrastructure;
using Xunit;

namespace Ordering.Integration.Tests;

// Only the test harness knows all three hosts. Each host receives its own restricted database role/schema.
public sealed class CheckoutFixture : IAsyncLifetime
{
    private readonly List<(string Connection, string Schema)> schemas = [];
    public WebApplicationFactory<CatalogApi::Program> Catalog { get; private set; } = null!;
    public WebApplicationFactory<InventoryApi::Program> Inventory { get; private set; } = null!;
    public WebApplicationFactory<Program> Ordering { get; private set; } = null!;
    public HttpClient CatalogHttp { get; private set; } = null!;
    public HttpClient InventoryHttp { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        try
        {
            Catalog = Create<CatalogApi::Program>(await CreateSchema("catalog"));
            Catalog.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
            CatalogHttp = Catalog.CreateClient();
            await using (var scope = Catalog.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<Catalog.Infrastructure.CatalogDbContext>().Database.MigrateAsync();
                await scope.ServiceProvider.GetRequiredService<Catalog.Infrastructure.CatalogSeedData>().SeedAsync(default);
            }
            Inventory = Create<InventoryApi::Program>(await CreateSchema("inventory"));
            Inventory.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
            InventoryHttp = Inventory.CreateClient();
            await using (var scope = Inventory.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<Inventory.Infrastructure.InventoryDbContext>().Database.MigrateAsync();
                await scope.ServiceProvider.GetRequiredService<Inventory.Infrastructure.InventorySeedData>().SeedAsync(default);
            }
            Ordering = Create<Program>(await CreateSchema("ordering"), new()
            {
                ["Services:Catalog:BaseUrl"] = CatalogHttp.BaseAddress!.ToString(),
                ["Services:Inventory:BaseUrl"] = InventoryHttp.BaseAddress!.ToString()
            });
            Ordering.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
            Client = Ordering.CreateClient();
            await using var orderingScope = Ordering.Services.CreateAsyncScope();
            await orderingScope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.MigrateAsync();
        }
        catch { await DisposeAsync(); throw; }
    }

    private static WebApplicationFactory<T> Create<T>(string connection, Dictionary<string, string?>? extra = null) where T : class
    {
        var settings = extra ?? new();
        settings["ConnectionStrings:Database"] = connection;
        return new WebApplicationFactory<T>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
        });
    }

    private async Task<string> CreateSchema(string service)
    {
        var raw = Environment.GetEnvironmentVariable(service.ToUpperInvariant() + "_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Run scripts/Test-All.ps1 with all three service test connections.");
        var settings = new NpgsqlConnectionStringBuilder(raw);
        if (settings.Database != service + "_db" || settings.Username != service + "_app")
            throw new InvalidOperationException("Checkout tests require the matching service role/database.");
        var schema = service + "_test_" + Guid.NewGuid().ToString("N");
        settings.SearchPath = schema;
        await Execute(settings.ConnectionString, $"CREATE SCHEMA \"{schema}\"");
        schemas.Add((settings.ConnectionString, schema));
        return settings.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        InventoryHttp?.Dispose();
        CatalogHttp?.Dispose();
        if (Ordering is not null) await Ordering.DisposeAsync();
        if (Inventory is not null) await Inventory.DisposeAsync();
        if (Catalog is not null) await Catalog.DisposeAsync();
        foreach (var (connection, schema) in schemas)
            await Execute(connection, $"DROP SCHEMA \"{schema}\" CASCADE");
        schemas.Clear();
    }

    private static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
