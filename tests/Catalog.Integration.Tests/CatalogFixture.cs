using Catalog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Catalog.Integration.Tests;

public sealed class CatalogFixture : IAsyncLifetime
{
    private readonly string schema = "catalog_test_" + Guid.NewGuid().ToString("N");
    private string connectionString = "";
    private bool schemaCreated;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var raw = Environment.GetEnvironmentVariable("CATALOG_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Run scripts/Test-Catalog.ps1 or set CATALOG_TEST_CONNECTION_STRING to the local catalog_app/catalog_db connection.");
        var settings = new NpgsqlConnectionStringBuilder(raw);
        if (settings.Database != "catalog_db" || settings.Username != "catalog_app")
            throw new InvalidOperationException("Tests require the Catalog database and application role.");
        settings.SearchPath = schema;
        connectionString = settings.ConnectionString;
        try
        {
            await ExecuteAsync($"CREATE SCHEMA \"{schema}\"");
            schemaCreated = true;
            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ConnectionStrings:Database"] = connectionString }));
            });
            Client = Factory.CreateClient();
            await using var scope = Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await db.Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<CatalogSeedData>().SeedAsync(default);
        }
        catch { await DisposeAsync(); throw; }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null) await Factory.DisposeAsync();
        // The identifier is generated locally, never taken from configuration or a request.
        // SearchPath excludes public, including for EF's migration history table.
        if (schemaCreated)
        {
            await ExecuteAsync($"DROP SCHEMA \"{schema}\" CASCADE");
            schemaCreated = false;
        }
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
