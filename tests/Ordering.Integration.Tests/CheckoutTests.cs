using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordering.Application;
using Ordering.Infrastructure;
using Xunit;

namespace Ordering.Integration.Tests;

public sealed class CheckoutTests(CheckoutFixture fixture) : IClassFixture<CheckoutFixture>
{
    private static readonly Guid Laptop = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid Mouse = Guid.Parse("22222222-2222-2222-2222-222222222223");
    private static CheckoutRequest Request(params CheckoutItem[] items) => new(Guid.NewGuid(), items);

    [Fact]
    public async Task Real_HTTP_checkout_persists_server_snapshots_history_and_does_not_change_stock()
    {
        var before = await fixture.InventoryHttp.GetStringAsync($"/api/inventory/items/{Laptop}");
        var request = Request(new CheckoutItem(Laptop, 1), new(Laptop, 1), new(Mouse, 3));
        using var response = await fixture.Client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<OrderDetailsDto>())!;
        Assert.Equal($"/api/orders/{created.Id}", response.Headers.Location!.ToString());
        Assert.Equal("Pending", created.Status);
        Assert.Equal(2089.95m, created.Total);
        Assert.Equal("USD", created.Currency);
        Assert.Equal(2, created.Items.Count);
        Assert.Equal(2, created.Items.Single(x => x.ProductId == Laptop).Quantity);
        var details = (await fixture.Client.GetFromJsonAsync<OrderDetailsDto>(response.Headers.Location))!;
        Assert.Equal(created.Items, details.Items);
        var history = (await fixture.Client.GetFromJsonAsync<OrderPage>($"/api/orders?customerId={request.CustomerId}"))!;
        Assert.Equal(created.Id, Assert.Single(history.Items).Id);
        Assert.Equal(before, await fixture.InventoryHttp.GetStringAsync($"/api/inventory/items/{Laptop}"));
    }

    [Fact]
    public async Task Catalog_edits_change_future_checkout_prices_but_not_existing_snapshots()
    {
        using var productResponse = await fixture.CatalogHttp.PostAsJsonAsync("/api/catalog/products", new
        {
            name = "Checkout snapshot", description = "Test", unitPrice = 12.34m,
            categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        });
        productResponse.EnsureSuccessStatusCode();
        var product = (await productResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await fixture.InventoryHttp.PostAsJsonAsync("/api/inventory/items", new { productId = product, onHand = 5 })).EnsureSuccessStatusCode();
        var original = (await (await fixture.Client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(product, 2)))).Content.ReadFromJsonAsync<OrderDetailsDto>())!;
        (await fixture.CatalogHttp.PutAsJsonAsync($"/api/catalog/products/{product}", new
        {
            name = "New name", description = "Changed", unitPrice = 25m,
            categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        })).EnsureSuccessStatusCode();
        var next = (await (await fixture.Client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(product, 2)))).Content.ReadFromJsonAsync<OrderDetailsDto>())!;
        var old = (await fixture.Client.GetFromJsonAsync<OrderDetailsDto>($"/api/orders/{original.Id}"))!;
        Assert.Equal(24.68m, old.Total);
        Assert.Equal("Checkout snapshot", Assert.Single(old.Items).ProductName);
        Assert.Equal(50m, next.Total);
        Assert.Equal("New name", Assert.Single(next.Items).ProductName);
    }

    [Fact]
    public async Task Missing_product_missing_stock_and_combined_insufficiency_never_save_partial_orders()
    {
        var before = await Count();
        await Problem(await fixture.Client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(Mouse, 1), new(Guid.NewGuid(), 1))), 409);
        await Problem(await fixture.Client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(Laptop, 6), new(Laptop, 5))), 409);
        using var product = await fixture.CatalogHttp.PostAsJsonAsync("/api/catalog/products", new
        {
            name = "No inventory", description = "Test", unitPrice = 1m,
            categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        });
        product.EnsureSuccessStatusCode();
        var id = (await product.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Problem(await fixture.Client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(Mouse, 1), new(id, 1))), 409);
        Assert.Equal(before, await Count());
    }

    [Theory]
    [InlineData("{\"customerId\":\"33333333-3333-3333-3333-333333333331\",\"items\":[{\"productId\":\"22222222-2222-2222-2222-222222222221\",\"quantity\":1,\"unitPrice\":0.01}]}")]
    [InlineData("{\"customerId\":\"33333333-3333-3333-3333-333333333331\",\"items\":[],\"status\":\"Confirmed\"}")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{bad json")]
    public async Task Invalid_or_untrusted_checkout_fields_return_400(string json)
    {
        var before = await Count();
        await Problem(await fixture.Client.PostAsync("/api/orders", new StringContent(json, Encoding.UTF8, "application/json")), 400);
        Assert.Equal(before, await Count());
    }

    [Theory]
    [InlineData("Catalog", "status", 503)]
    [InlineData("Inventory", "status", 503)]
    [InlineData("Catalog", "redirect", 503)]
    [InlineData("Catalog", "json", 502)]
    [InlineData("Catalog", "missing-price", 502)]
    [InlineData("Catalog", "currency", 502)]
    [InlineData("Catalog", "wrong-id", 502)]
    [InlineData("Catalog", "price-precision", 502)]
    [InlineData("Catalog", "empty-name", 502)]
    [InlineData("Catalog", "null", 502)]
    [InlineData("Catalog", "media", 502)]
    [InlineData("Inventory", "inconsistent", 502)]
    [InlineData("Inventory", "missing-available", 502)]
    [InlineData("Inventory", "negative", 502)]
    [InlineData("Inventory", "wrong-id", 502)]
    [InlineData("Catalog", "slow-headers", 504)]
    [InlineData("Inventory", "slow-body", 504)]
    [InlineData("Catalog", "oversized", 503)]
    public async Task Dependency_failures_are_bounded_sanitized_and_never_save(string service, string mode, int expected)
    {
        var calls = 0;
        await using var remote = await StartRemote(async context =>
        {
            Interlocked.Increment(ref calls);
            if (mode == "slow-headers") await Task.Delay(Timeout.Infinite, context.RequestAborted);
            if (mode == "status") { context.Response.StatusCode = 500; await context.Response.WriteAsync("secret downstream internals"); return; }
            if (mode == "redirect") { context.Response.Redirect("/redirect-target"); return; }
            context.Response.ContentType = mode == "media" ? "text/html" : "application/json";
            if (mode == "slow-body")
            {
                await context.Response.WriteAsync("{");
                await context.Response.Body.FlushAsync();
                await Task.Delay(Timeout.Infinite, context.RequestAborted);
            }
            var id = mode == "wrong-id" ? Guid.NewGuid() : Laptop;
            var body = mode switch
            {
                "json" => "{broken secret downstream internals",
                "null" => "null",
                "oversized" => new string('x', 70000),
                "missing-price" => JsonSerializer.Serialize(new { id, name = "P", currency = "USD" }),
                "missing-available" => JsonSerializer.Serialize(new { productId = id, onHand = 10, reserved = 0 }),
                _ when service == "Catalog" => JsonSerializer.Serialize(new { id, name = mode == "empty-name" ? "" : "P", unitPrice = mode == "price-precision" ? 1.001m : 1m, currency = mode == "currency" ? "EUR" : "USD" }),
                _ => JsonSerializer.Serialize(new { productId = id, onHand = 10, reserved = 0, available = mode == "negative" ? -1 : mode == "inconsistent" ? 11 : 10 })
            };
            await context.Response.WriteAsync(body);
        });
        await using var factory = Override(service, remote.Urls.Single(), 200);
        using var client = factory.CreateClient();
        var before = await Count();
        await Problem(await client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(Laptop, 1))), expected);
        Assert.Equal(1, calls); // No hidden retry or redirect follow-up.
        Assert.Equal(before, await Count());
    }

    [Fact]
    public async Task Connection_failure_is_503_without_saving()
    {
        var remote = await StartRemote(_ => Task.CompletedTask);
        var url = remote.Urls.Single();
        await remote.DisposeAsync();
        // Allow the OS to report connection refusal before the independent HTTP timeout expires.
        await using var factory = Override("Catalog", url, 5000);
        using var client = factory.CreateClient();
        var before = await Count();
        await Problem(await client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(Laptop, 1))), 503);
        Assert.Equal(before, await Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Overall_deadline_and_caller_cancellation_reach_the_downstream_and_prevent_writes(bool callerCancels)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var remote = await StartRemote(async context =>
        {
            started.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, context.RequestAborted); }
            catch (OperationCanceledException) { aborted.TrySetResult(); }
        });
        await using var factory = Override("Catalog", remote.Urls.Single(), 5000, 1000);
        factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        // A derived factory initially copies its parent's client URL. Start it before creating the client
        // so the newly allocated loopback address, rather than the parent's live host, is selected.
        factory.StartServer();
        using var client = factory.CreateClient();
        using var caller = new CancellationTokenSource();
        var before = await Count();
        var pending = client.PostAsJsonAsync("/api/orders", Request(new CheckoutItem(Laptop, 1)), caller.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (callerCancels)
        {
            caller.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
        else await Problem(await pending, 504);
        await aborted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(before, await Count());
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Override(string service, string url, int timeout, int overall = 15000) =>
        fixture.Ordering.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"Services:{service}:BaseUrl"] = url,
                [$"Services:{service}:TimeoutMilliseconds"] = timeout.ToString(),
                ["Services:CheckoutTimeoutMilliseconds"] = overall.ToString()
            })));

    private static async Task<WebApplication> StartRemote(RequestDelegate handler)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.Run(handler);
        await app.StartAsync();
        return app;
    }

    private async Task<int> Count()
    {
        await using var scope = fixture.Ordering.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Orders.CountAsync();
    }

    private static async Task Problem(HttpResponseMessage response, int expected)
    {
        using (response)
        {
            Assert.Equal(expected, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            Assert.Equal(expected, json.RootElement.GetProperty("status").GetInt32());
            Assert.True(json.RootElement.TryGetProperty("traceId", out _));
            Assert.DoesNotContain("secret downstream", text);
            Assert.DoesNotContain("Password=", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
