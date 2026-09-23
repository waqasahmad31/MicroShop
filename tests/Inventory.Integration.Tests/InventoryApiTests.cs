using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Inventory.Application;
using Inventory.Domain;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Inventory.Integration.Tests;

public sealed class InventoryApiTests(InventoryFixture fixture) : IClassFixture<InventoryFixture>
{
    private const string Items = "/api/inventory/items";
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task Create_adjust_and_read_stock_without_a_Catalog_dependency()
    {
        var id = Guid.NewGuid(); // Deliberately not a Catalog seed ID.
        var response = await Client.PostAsJsonAsync(Items, new CreateInventoryItemRequest(id, 10));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Items}/{id}", response.Headers.Location!.OriginalString);
        var created = (await response.Content.ReadFromJsonAsync<InventoryItemDto>())!;
        Assert.Equal(new InventoryItemDto(id, 10, 0, 10), created);
        Assert.Equal(HttpStatusCode.OK, (await Adjust(id, 5)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Adjust(id, -15)).StatusCode);
        Assert.Equal(new InventoryItemDto(id, 0, 0, 0), await Get(id));
        await Problem(await Adjust(id, -1), HttpStatusCode.Conflict);
        Assert.Equal(0, (await Get(id)).OnHand);
        Assert.Equal(HttpStatusCode.OK, (await Adjust(id, 1)).StatusCode); // Failed transaction released its lock.
    }

    [Fact]
    public async Task Concurrent_creates_for_one_product_return_one_conflict()
    {
        var id = Guid.NewGuid();
        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync(Items, new CreateInventoryItemRequest(id, 3)),
            Client.PostAsJsonAsync(Items, new CreateInventoryItemRequest(id, 3)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        await Problem(Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
        Assert.Equal(3, (await Get(id)).OnHand);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{broken")]
    [InlineData("{\"productId\":\"not-a-guid\",\"onHand\":1}")]
    [InlineData("{\"productId\":\"00000000-0000-0000-0000-000000000000\",\"onHand\":1}")]
    [InlineData("{\"productId\":\"99999999-9999-9999-9999-999999999999\"}")]
    [InlineData("{\"productId\":\"99999999-9999-9999-9999-999999999999\",\"onHand\":-1}")]
    [InlineData("{\"productId\":\"99999999-9999-9999-9999-999999999999\",\"onHand\":1.5}")]
    public async Task Invalid_create_returns_problem_details(string json) =>
        await Problem(await Client.PostAsync(Items, Json(json)), HttpStatusCode.BadRequest);

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"delta\":0}")]
    [InlineData("{\"delta\":1.5}")]
    [InlineData("{\"delta\":2147483648}")]
    public async Task Invalid_adjustment_does_not_change_stock(string json)
    {
        var id = await Create(5);
        await Problem(await Client.PostAsync($"{Items}/{id}/adjustments", Json(json)), HttpStatusCode.BadRequest);
        Assert.Equal(5, (await Get(id)).OnHand);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=bad")]
    public async Task Invalid_pagination_returns_problem(string query) =>
        await Problem(await Client.GetAsync($"{Items}?{query}"), HttpStatusCode.BadRequest);

    [Fact]
    public async Task Details_pagination_and_missing_items()
    {
        var page = (await Client.GetFromJsonAsync<InventoryPage>($"{Items}?page=1&pageSize=2"))!;
        var next = (await Client.GetFromJsonAsync<InventoryPage>($"{Items}?page=2&pageSize=2"))!;
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, next.Items.Count);
        Assert.True(page.TotalCount >= 5);
        Assert.Empty(page.Items.Select(x => x.ProductId).Intersect(next.Items.Select(x => x.ProductId)));
        var empty = (await Client.GetFromJsonAsync<InventoryPage>($"{Items}?page=2147483647&pageSize=100"))!;
        Assert.Empty(empty.Items);
        Assert.Equal(page.TotalCount, empty.TotalCount);
        await Problem(await Client.GetAsync($"{Items}/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
        await Problem(await Adjust(Guid.NewGuid(), 1), HttpStatusCode.NotFound);
        await Problem(await Client.GetAsync($"{Items}/{Guid.Empty}"), HttpStatusCode.BadRequest);
        await Problem(await Client.GetAsync("/missing-route"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Concurrent_increments_across_two_hosts_never_lose_updates()
    {
        var id = await Create(0);
        await using var otherHost = fixture.Factory.WithWebHostBuilder(_ => { });
        using var otherClient = otherHost.CreateClient();
        var responses = await Task.WhenAll(Enumerable.Range(0, 32).Select(i =>
            (i % 2 == 0 ? Client : otherClient).PostAsJsonAsync($"{Items}/{id}/adjustments", new AdjustStockRequest(1))));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var reported = new List<int>();
        foreach (var response in responses)
            reported.Add((await response.Content.ReadFromJsonAsync<InventoryItemDto>())!.OnHand);
        Assert.Equal(Enumerable.Range(1, 32), reported.Order());
        Assert.Equal(32, (await Get(id)).OnHand);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task Competing_decrements_cannot_make_stock_negative_or_consume_reserved(int reserved)
    {
        var id = Guid.NewGuid();
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            // A nonzero reserved state is set up directly; Phase 4 exposes no reservation API.
            db.Items.Add(new InventoryItem(id, 10, reserved));
            await db.SaveChangesAsync();
        }
        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Adjust(id, -1)));
        Assert.Equal(10 - reserved, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(6 + reserved, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        foreach (var rejected in responses.Where(r => r.StatusCode == HttpStatusCode.Conflict))
            await Problem(rejected, HttpStatusCode.Conflict);
        Assert.Equal(new InventoryItemDto(id, reserved, reserved, 0), await Get(id));
    }

    [Fact]
    public async Task Writer_waits_for_database_lock_and_then_uses_latest_committed_quantity()
    {
        var id = await Create(10);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var locked = await db.Items.FromSql($"SELECT * FROM inventory_items WHERE product_id = {id} FOR UPDATE").SingleAsync();
        var blockerPid = ((NpgsqlConnection)db.Database.GetDbConnection()).ProcessID;
        var pending = Adjust(id, -6);
        try
        {
            await WaitForBlockedWriter(blockerPid);
            locked.AdjustOnHand(-5);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        finally
        {
            // Disposal also releases the lock if an assertion/timeout interrupts the test.
            await transaction.DisposeAsync();
        }
        await Problem(await pending.WaitAsync(TimeSpan.FromSeconds(10)), HttpStatusCode.Conflict);
        Assert.Equal(5, (await Get(id)).OnHand);
    }

    [Fact]
    public async Task Overflow_is_a_conflict_and_preserves_quantity()
    {
        var id = await Create(int.MaxValue);
        await Problem(await Adjust(id, 1), HttpStatusCode.Conflict);
        Assert.Equal(int.MaxValue, (await Get(id)).Available);
    }

    [Fact]
    public async Task Database_constraints_reject_invalid_stock_even_outside_domain()
    {
        var id = await Create(5);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        foreach (var values in new[] { (OnHand: -1, Reserved: 0), (OnHand: 5, Reserved: -1), (OnHand: 5, Reserved: 6) })
        {
            var error = await Assert.ThrowsAsync<PostgresException>(async () =>
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE inventory_items SET on_hand = {values.OnHand}, reserved = {values.Reserved} WHERE product_id = {id}"));
            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }
        Assert.Equal(new InventoryItemDto(id, 5, 0, 5), await Get(id));
    }

    [Fact]
    public async Task Seed_preserves_adjusted_stock_and_migration_history_is_isolated()
    {
        var id = InventorySeedData.LaptopId;
        var original = await Get(id);
        Assert.Equal(HttpStatusCode.OK, (await Adjust(id, 2)).StatusCode);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var count = await db.Items.CountAsync();
        var seed = scope.ServiceProvider.GetRequiredService<InventorySeedData>();
        await seed.SeedAsync(default);
        await seed.SeedAsync(default);
        Assert.Equal(count, await db.Items.CountAsync());
        Assert.Equal(original.OnHand + 2, (await Get(id)).OnHand);
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.StartsWith("inventory_test_", await db.Database.SqlQueryRaw<string>(
            "SELECT current_schema() AS \"Value\"").SingleAsync());
    }

    [Fact]
    public async Task Development_openapi_and_swagger_describe_adjustments()
    {
        var doc = await Client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/inventory/items/{productId}/adjustments", doc);
        Assert.Contains("swagger-ui", await Client.GetStringAsync("/swagger/index.html"));
    }

    private async Task WaitForBlockedWriter(int blockerPid)
    {
        var source = fixture.Factory.Services.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await source.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*) FROM pg_stat_activity
            WHERE datname = current_database() AND @pid = ANY(pg_blocking_pids(pid))
            """, connection);
        command.Parameters.AddWithValue("pid", blockerPid);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if ((long)(await command.ExecuteScalarAsync())! > 0) return;
            await Task.Delay(20);
        }
        Assert.Fail("The adjustment did not wait for the existing row lock.");
    }

    private async Task<Guid> Create(int onHand)
    {
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync(Items, new CreateInventoryItemRequest(id, onHand))).StatusCode);
        return id;
    }
    private async Task<InventoryItemDto> Get(Guid id) => (await Client.GetFromJsonAsync<InventoryItemDto>($"{Items}/{id}"))!;
    private Task<HttpResponseMessage> Adjust(Guid id, int delta) =>
        Client.PostAsJsonAsync($"{Items}/{id}/adjustments", new AdjustStockRequest(delta));
    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        Assert.Equal((int)status, doc.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("stackTrace", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", text, StringComparison.OrdinalIgnoreCase);
    }
}
