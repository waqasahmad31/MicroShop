using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ordering.Application;
using Ordering.Domain;
using Ordering.Infrastructure;
using Xunit;

namespace Ordering.Integration.Tests;

public sealed class OrderingTests(OrderingFixture fixture) : IClassFixture<OrderingFixture>
{
    private const string Orders = "/api/orders";
    private HttpClient Client => fixture.Client;

    [Fact]
    public async Task EF_aggregate_round_trip_and_Dapper_details_preserve_priced_snapshots()
    {
        var product = Guid.NewGuid();
        var lines = new List<PricedOrderLine> { new(product, "Historical product", 12.34m, 2), new(product, "Historical product", 12.34m, 1) };
        var created = await Create(Guid.NewGuid(), lines);
        lines[0] = new(product, "Changed outside order", 99.99m, 1);
        var detail = await Get(created.Id);
        var item = Assert.Single(detail.Items);
        Assert.Equal("Historical product", item.ProductName);
        Assert.Equal(12.34m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(37.02m, detail.Total);
        Assert.Equal(detail.Total, detail.Items.Sum(x => x.LineTotal));
        Assert.Equal("USD", detail.Currency);
        Assert.Equal("Pending", detail.Status);
        Assert.Equal(DateTimeKind.Utc, detail.CreatedAtUtc.Kind);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var entity = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == created.Id);
        Assert.Equal(detail.Total, entity.Total);
        Assert.Equal(item.ProductName, Assert.Single(entity.Items).ProductName);
    }

    [Fact]
    public async Task History_is_customer_scoped_paginated_and_stably_ordered()
    {
        var customer = Guid.NewGuid();
        var time = new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc);
        var ids = new[] {
            Guid.Parse("55555555-5555-5555-5555-555555555551"),
            Guid.Parse("55555555-5555-5555-5555-555555555552"),
            Guid.Parse("55555555-5555-5555-5555-555555555553") };
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            foreach (var id in ids)
                db.Orders.Add(new Order(id, customer, time, [new OrderItem(Guid.NewGuid(), "Item", 0.10m, 3)]));
            db.Orders.Add(new Order(Guid.NewGuid(), Guid.NewGuid(), time.AddDays(1), [new OrderItem(Guid.NewGuid(), "Other customer", 99m, 1)]));
            await db.SaveChangesAsync();
        }
        var first = (await Client.GetFromJsonAsync<OrderPage>($"{Orders}?customerId={customer}&pageSize=2"))!;
        var next = (await Client.GetFromJsonAsync<OrderPage>($"{Orders}?customerId={customer}&pageSize=2&page=2"))!;
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(new[] { ids[2], ids[1] }, first.Items.Select(x => x.Id));
        Assert.Equal(ids[0], Assert.Single(next.Items).Id);
        Assert.All(first.Items, x => { Assert.Equal(customer, x.CustomerId); Assert.Equal(0.30m, x.Total); });
        Assert.Empty((await Client.GetFromJsonAsync<OrderPage>($"{Orders}?customerId={customer}&page=2147483647&pageSize=100"))!.Items);
        Assert.Empty((await Client.GetFromJsonAsync<OrderPage>($"{Orders}?customerId={Guid.NewGuid()}"))!.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("customerId=invalid")]
    [InlineData("customerId=00000000-0000-0000-0000-000000000000")]
    [InlineData("customerId=33333333-3333-3333-3333-333333333331&page=0")]
    [InlineData("customerId=33333333-3333-3333-3333-333333333331&pageSize=101")]
    [InlineData("customerId=33333333-3333-3333-3333-333333333331&page=bad")]
    public async Task Invalid_history_input_returns_problem(string query) =>
        await Problem(await Client.GetAsync($"{Orders}?{query}"), HttpStatusCode.BadRequest);

    [Fact]
    public async Task Missing_and_empty_order_ids_are_reported()
    {
        await Problem(await Client.GetAsync($"{Orders}/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
        await Problem(await Client.GetAsync($"{Orders}/{Guid.Empty}"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Public_price_submissions_and_status_mutations_are_not_exposed()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var before = await db.Orders.CountAsync();
        var payload = new { customerId = OrderingSeedData.CustomerId, items = new[] { new { productId = Guid.NewGuid(), unitPrice = 0.01m, quantity = 1 } } };
        await Problem(await Client.PostAsJsonAsync(Orders, payload), HttpStatusCode.MethodNotAllowed);
        await Problem(await Client.PutAsJsonAsync($"{Orders}/{OrderingSeedData.PendingOrderId}", new { status = "Confirmed" }), HttpStatusCode.MethodNotAllowed);
        await Problem(await Client.PostAsJsonAsync($"{Orders}/{OrderingSeedData.PendingOrderId}/cancel", new { }), HttpStatusCode.NotFound);
        Assert.Equal(before, await db.Orders.CountAsync());
        using var openapi = JsonDocument.Parse(await Client.GetStringAsync("/openapi/v1.json"));
        foreach (var path in openapi.RootElement.GetProperty("paths").EnumerateObject())
        {
            Assert.False(path.Value.TryGetProperty("post", out _));
            Assert.False(path.Value.TryGetProperty("put", out _));
            Assert.False(path.Value.TryGetProperty("patch", out _));
            Assert.False(path.Value.TryGetProperty("delete", out _));
        }
        Assert.Contains("swagger-ui", await Client.GetStringAsync("/swagger/index.html"));
        Assert.Null(scope.ServiceProvider.GetService<ICatalogServiceClient>());
        Assert.Null(scope.ServiceProvider.GetService<IInventoryServiceClient>());
    }

    [Fact]
    public async Task Rejected_item_rolls_back_entire_aggregate()
    {
        var id = Guid.NewGuid();
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            var order = new Order(id, Guid.NewGuid(), DateTime.UtcNow,
                [new OrderItem(Guid.NewGuid(), "Valid", 5, 1), new OrderItem(Guid.NewGuid(), "Invalid at SQL boundary", 5, 1)]);
            db.Orders.Add(order);
            // Bypass Domain intentionally to exercise a database failure inside the aggregate transaction.
            db.Entry(order.Items[1]).Property(x => x.Quantity).CurrentValue = 0;
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.CheckViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
        }
        await using var verify = fixture.Factory.Services.CreateAsyncScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<OrderingDbContext>();
        Assert.False(await verifyDb.Orders.AnyAsync(x => x.Id == id));
        Assert.False(await verifyDb.OrderItems.AnyAsync(x => x.OrderId == id));
    }

    [Fact]
    public async Task Transitions_persist_without_mutating_snapshots_and_replays_preserve_reason()
    {
        var created = await Create(Guid.NewGuid(), [new(Guid.NewGuid(), "Snapshot", 19.99m, 2)]);
        await Transition(created.Id, OrderStatus.Rejected, " Original failure ");
        await Transition(created.Id, OrderStatus.Rejected, "Replacement");
        var rejected = await Get(created.Id);
        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal("Original failure", rejected.StatusReason);
        Assert.Equal(created.Total, rejected.Total);
        Assert.Equal(created.Items, rejected.Items);
        await Assert.ThrowsAsync<OrderTransitionException>(() => Transition(created.Id, OrderStatus.Confirmed));
        Assert.Equal("Rejected", (await Get(created.Id)).Status);
        var other = await Create(Guid.NewGuid(), [new(Guid.NewGuid(), "Other", 1, 1)]);
        await Transition(other.Id, OrderStatus.Confirmed);
        await Transition(other.Id, OrderStatus.Cancelled, "Requested");
        await Assert.ThrowsAsync<OrderTransitionException>(() => Transition(other.Id, OrderStatus.Confirmed));
        Assert.Equal("Cancelled", (await Get(other.Id)).Status);
    }

    [Fact]
    public async Task Concurrent_incompatible_outcomes_have_one_winner()
    {
        var order = await Create(Guid.NewGuid(), [new(Guid.NewGuid(), "Product", 1, 1)]);
        async Task<Exception?> Attempt(OrderStatus target, string? reason) =>
            await Record.ExceptionAsync(() => Transition(order.Id, target, reason));
        var results = await Task.WhenAll(Attempt(OrderStatus.Confirmed, null), Attempt(OrderStatus.Rejected, "Out of stock"));
        Assert.Single(results, x => x is null);
        Assert.IsType<OrderTransitionException>(Assert.Single(results, x => x is not null));
        Assert.Contains((await Get(order.Id)).Status, new[] { "Confirmed", "Rejected" });
    }

    [Fact]
    public async Task Tracked_stale_order_is_refreshed_before_transition()
    {
        var order = await Create(Guid.NewGuid(), [new(Guid.NewGuid(), "Product", 1, 1)]);
        await using var staleScope = fixture.Factory.Services.CreateAsyncScope();
        var db = staleScope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var tracked = await db.Orders.SingleAsync(x => x.Id == order.Id);
        Assert.Equal(OrderStatus.Pending, tracked.Status);
        await Transition(order.Id, OrderStatus.Cancelled);
        await Assert.ThrowsAsync<OrderTransitionException>(() =>
            staleScope.ServiceProvider.GetRequiredService<OrderingService>().TransitionAsync(order.Id, OrderStatus.Confirmed, null, default));
        Assert.Equal("Cancelled", (await Get(order.Id)).Status);
    }

    [Fact]
    public async Task Maximum_total_matches_domain_and_SQL_without_rounding()
    {
        var order = await Create(Guid.NewGuid(), Enumerable.Range(0, 100)
            .Select(_ => new PricedOrderLine(Guid.NewGuid(), "Maximum", 99_999_999.99m, 1000)).ToArray());
        var detail = await Get(order.Id);
        Assert.Equal(9_999_999_999_000m, detail.Total);
        Assert.Equal(order.Total, detail.Total);
        Assert.Equal(100, detail.Items.Count);
    }

    [Fact]
    public async Task Seed_preserves_state_and_uses_independent_migration_history()
    {
        await Transition(OrderingSeedData.PendingOrderId, OrderStatus.Cancelled, "Preserve this change");
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var before = await db.Orders.CountAsync();
        var seed = scope.ServiceProvider.GetRequiredService<OrderingSeedData>();
        await seed.SeedAsync(default);
        await seed.SeedAsync(default);
        Assert.Equal(before, await db.Orders.CountAsync());
        var order = await Get(OrderingSeedData.PendingOrderId);
        Assert.Equal("Cancelled", order.Status);
        Assert.Equal("Preserve this change", order.StatusReason);
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.StartsWith("ordering_test_", await db.Database.SqlQueryRaw<string>(
            "SELECT current_schema() AS \"Value\"").SingleAsync());
    }

    private async Task<OrderDetailsDto> Create(Guid customer, IReadOnlyList<PricedOrderLine> lines)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OrderingService>().CreateFromPricedLinesAsync(customer, lines, default);
    }
    private async Task Transition(Guid id, OrderStatus target, string? reason = null)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrderingService>().TransitionAsync(id, target, reason, default);
    }
    private async Task<OrderDetailsDto> Get(Guid id) => (await Client.GetFromJsonAsync<OrderDetailsDto>($"{Orders}/{id}"))!;
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal((int)expected, doc.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("stackTrace", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", json, StringComparison.OrdinalIgnoreCase);
    }
}
