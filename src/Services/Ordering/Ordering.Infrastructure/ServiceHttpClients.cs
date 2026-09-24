using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ordering.Application;
using Ordering.Domain;

namespace Ordering.Infrastructure;

public sealed class CatalogServiceClient(HttpClient http) : ICatalogServiceClient
{
    public async Task<CatalogProductSnapshot?> GetProductAsync(Guid productId, CancellationToken ct)
    {
        var product = await ServiceJson.GetAsync<ProductResponse>(http, $"api/catalog/products/{productId}", "Catalog", ct);
        if (product is null) return null;
        if (product.Id != productId || product.UnitPrice is null || product.Currency != "USD")
            throw new OrderingDependencyException("Catalog", DependencyFailure.InvalidResponse);
        try
        {
            var validated = new OrderItem(productId, product.Name, product.UnitPrice.Value, 1);
            return new(productId, validated.ProductName, validated.UnitPrice, product.Currency);
        }
        catch (OrderingValidationException)
        {
            throw new OrderingDependencyException("Catalog", DependencyFailure.InvalidResponse);
        }
    }

    private sealed record ProductResponse(Guid? Id, string? Name, decimal? UnitPrice, string? Currency);
}

public sealed class InventoryServiceClient(HttpClient http) : IInventoryServiceClient
{
    public async Task<InventoryAvailability?> GetAvailabilityAsync(Guid productId, CancellationToken ct)
    {
        var stock = await ServiceJson.GetAsync<StockResponse>(http, $"api/inventory/items/{productId}", "Inventory", ct);
        if (stock is null) return null;
        if (stock.ProductId != productId || stock.OnHand is null or < 0 || stock.Reserved is null or < 0
            || stock.Available is null or < 0 || stock.Reserved > stock.OnHand
            || stock.Available != stock.OnHand - stock.Reserved)
            throw new OrderingDependencyException("Inventory", DependencyFailure.InvalidResponse);
        return new(productId, stock.Available.Value);
    }

    private sealed record StockResponse(Guid? ProductId, int? OnHand, int? Reserved, int? Available);
}

internal static class ServiceJson
{
    public static async Task<T?> GetAsync<T>(HttpClient http, string path, string service, CancellationToken ct) where T : class
    {
        try
        {
            // ResponseContentRead includes the bounded body download in HttpClient.Timeout.
            using var response = await http.GetAsync(path, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new OrderingDependencyException(service, DependencyFailure.Unavailable);
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType != "application/json")
                throw new OrderingDependencyException(service, DependencyFailure.InvalidResponse);
            return await response.Content.ReadFromJsonAsync<T>(ct)
                ?? throw new OrderingDependencyException(service, DependencyFailure.InvalidResponse);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new OrderingDependencyException(service, DependencyFailure.Timeout);
        }
        catch (HttpRequestException)
        {
            throw new OrderingDependencyException(service, DependencyFailure.Unavailable);
        }
        catch (JsonException)
        {
            throw new OrderingDependencyException(service, DependencyFailure.InvalidResponse);
        }
    }
}
