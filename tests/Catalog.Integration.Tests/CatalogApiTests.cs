using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Catalog.Application;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalog.Integration.Tests;

public sealed class CatalogApiTests(CatalogFixture fixture) : IClassFixture<CatalogFixture>
{
    private HttpClient Client => fixture.Client;
    private const string Products = "/api/catalog/products";
    private const string Categories = "/api/catalog/categories";

    [Fact]
    public async Task Crud_round_trip_uses_EF_writes_and_Dapper_reads_and_restricts_category_deletion()
    {
        var categoryResponse = await Client.PostAsJsonAsync(Categories, new CategoryRequest("Round trip", "Test"));
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = (await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>())!;
        var created = await Client.PostAsJsonAsync(Products, new ProductRequest("Test mouse", null, 12.34m, category.Id));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = (await created.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal($"{Products}/{product.Id}", created.Headers.Location!.OriginalString);
        var fetched = (await Client.GetFromJsonAsync<ProductDto>(created.Headers.Location))!;
        Assert.Equal(12.34m, fetched.UnitPrice);
        Assert.Equal("Round trip", fetched.CategoryName);
        Assert.Equal("USD", fetched.Currency);
        await Problem(await Client.DeleteAsync($"{Categories}/{category.Id}"), HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.OK, (await Client.PutAsJsonAsync($"{Categories}/{category.Id}", new CategoryRequest("Renamed", ""))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.PutAsJsonAsync($"{Products}/{product.Id}", new ProductRequest("Updated", "New", 0, category.Id))).StatusCode);
        fetched = (await Client.GetFromJsonAsync<ProductDto>($"{Products}/{product.Id}"))!;
        Assert.Equal("Updated", fetched.Name);
        Assert.Equal("Renamed", fetched.CategoryName);
        Assert.Equal(0, fetched.UnitPrice);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"{Products}/{product.Id}")).StatusCode);
        await Problem(await Client.GetAsync($"{Products}/{product.Id}"), HttpStatusCode.NotFound);
        await Problem(await Client.DeleteAsync($"{Products}/{product.Id}"), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"{Categories}/{category.Id}")).StatusCode);
    }

    [Fact]
    public async Task Concurrent_case_insensitive_category_duplicates_return_one_conflict()
    {
        var name = "Unique " + Guid.NewGuid().ToString("N");
        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync(Categories, new CategoryRequest(name, null)),
            Client.PostAsJsonAsync(Categories, new CategoryRequest(name.ToUpperInvariant(), null)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        await Problem(Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"name\":\"Mouse\",\"categoryId\":\"11111111-1111-1111-1111-111111111112\"}")]
    [InlineData("{\"name\":\"Mouse\",\"unitPrice\":-1,\"categoryId\":\"11111111-1111-1111-1111-111111111112\"}")]
    [InlineData("{\"name\":\"Mouse\",\"unitPrice\":1.001,\"categoryId\":\"11111111-1111-1111-1111-111111111112\"}")]
    [InlineData("{\"name\":\"Mouse\",\"unitPrice\":1,\"categoryId\":\"99999999-9999-9999-9999-999999999999\"}")]
    [InlineData("{broken")]
    public async Task Invalid_requests_return_problem_details(string json) =>
        await Problem(await Client.PostAsync(Products, new StringContent(json, Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=not-a-number")]
    [InlineData("categoryId=invalid")]
    public async Task Invalid_query_returns_problem_details(string query) =>
        await Problem(await Client.GetAsync($"{Products}?{query}"), HttpStatusCode.BadRequest);

    [Fact]
    public async Task Search_pagination_and_category_filter_are_parameterized()
    {
        var result = (await Client.GetFromJsonAsync<PagedResult<ProductDto>>($"{Products}?pageSize=2&categoryId={CatalogSeedData.ComputersId}"))!;
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, p => Assert.Equal(CatalogSeedData.ComputersId, p.CategoryId));
        var laptop = (await Client.GetFromJsonAsync<PagedResult<ProductDto>>($"{Products}?search=LAPTOP"))!;
        Assert.Equal(CatalogSeedData.LaptopId, Assert.Single(laptop.Items).Id);
        var empty = (await Client.GetFromJsonAsync<PagedResult<ProductDto>>($"{Products}?page=2147483647&pageSize=100"))!;
        Assert.Empty(empty.Items);
        foreach (var search in new[] { "%", "_", "' OR 1=1 --", "\\" })
        {
            var escaped = (await Client.GetFromJsonAsync<PagedResult<ProductDto>>($"{Products}?search={Uri.EscapeDataString(search)}"))!;
            Assert.Empty(escaped.Items);
        }
        var categories = (await Client.GetFromJsonAsync<PagedResult<CategoryDto>>($"{Categories}?search=computers&pageSize=1"))!;
        Assert.Equal(CatalogSeedData.ComputersId, Assert.Single(categories.Items).Id);
        Assert.NotNull(await Client.GetFromJsonAsync<PagedResult<CategoryDto>>(Categories));
    }

    [Fact]
    public async Task Missing_resources_and_routes_return_404()
    {
        var id = Guid.NewGuid();
        await Problem(await Client.GetAsync($"{Categories}/{id}"), HttpStatusCode.NotFound);
        await Problem(await Client.PutAsJsonAsync($"{Products}/{id}", new ProductRequest("Missing", null, 1, CatalogSeedData.ComputersId)), HttpStatusCode.NotFound);
        await Problem(await Client.PutAsJsonAsync($"{Categories}/{id}", new CategoryRequest("Missing", null)), HttpStatusCode.NotFound);
        await Problem(await Client.GetAsync("/not-a-route"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Seed_is_repeatable_and_preserves_edits_and_migration_history_is_isolated()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await db.Products.SingleAsync(p => p.Id == CatalogSeedData.LaptopId);
        product.Update(product.Name, "User-edited description", product.UnitPrice, product.CategoryId);
        await db.SaveChangesAsync();
        var before = await db.Products.CountAsync();
        await scope.ServiceProvider.GetRequiredService<CatalogSeedData>().SeedAsync(default);
        await scope.ServiceProvider.GetRequiredService<CatalogSeedData>().SeedAsync(default);
        Assert.Equal(before, await db.Products.CountAsync());
        Assert.Equal("User-edited description", (await Client.GetFromJsonAsync<ProductDto>($"{Products}/{product.Id}"))!.Description);
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        var schema = await db.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
        Assert.StartsWith("catalog_test_", schema);
    }

    [Fact]
    public async Task Development_openapi_and_swagger_describe_catalog()
    {
        var document = await Client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/catalog/products", document);
        Assert.Contains("/api/catalog/categories", document);
        Assert.Contains("swagger-ui", await Client.GetStringAsync("/swagger/index.html"));
    }

    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("stackTrace", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", json, StringComparison.OrdinalIgnoreCase);
    }
}
