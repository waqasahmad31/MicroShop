using Ordering.Application;
using Ordering.Domain;
using Xunit;

namespace Ordering.Unit.Tests;

public sealed class CheckoutTests
{
    private sealed class Dependencies : ICatalogServiceClient, IInventoryServiceClient, IOrderingWriter, IOrderingReader
    {
        public int Calls;
        public Order? Saved;
        public int Available = 1000;
        public Task<CatalogProductSnapshot?> GetProductAsync(Guid id, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<CatalogProductSnapshot?>(new(id, "Server name", 1.25m, "USD"));
        }
        public Task<InventoryAvailability?> GetAvailabilityAsync(Guid id, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<InventoryAvailability?>(new(id, Available));
        }
        public Task CreateAsync(Order order, CancellationToken ct) { Saved = order; return Task.CompletedTask; }
        public Task<bool> TransitionAsync(Guid id, OrderStatus target, string? reason, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderDetailsDto?> GetAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderPage> ListForCustomerAsync(Guid id, int page, int size, CancellationToken ct) => throw new NotSupportedException();
        public CheckoutService Service => new(this, this, new(this, this, TimeProvider.System));
    }

    [Fact]
    public async Task Duplicate_lines_merge_before_remote_checks_and_use_authoritative_snapshots()
    {
        var dependencies = new Dependencies();
        var id = Guid.NewGuid();
        var result = await dependencies.Service.CreateAsync(new(Guid.NewGuid(), [new(id, 2), new(id, 3)]), default);
        Assert.Equal(2, dependencies.Calls);
        Assert.Equal(5, Assert.Single(result.Items).Quantity);
        Assert.Equal("Server name", result.Items[0].ProductName);
        Assert.Equal(6.25m, result.Total);
        Assert.Equal(OrderStatus.Pending, dependencies.Saved!.Status);
    }

    [Fact]
    public async Task All_invalid_inputs_fail_before_network_or_persistence()
    {
        var id = Guid.NewGuid();
        CheckoutRequest?[] requests = [null, new(null, [new(id, 1)]), new(Guid.Empty, [new(id, 1)]),
            new(id, null), new(id, []), new(id, [null]), new(id, [new(null, 1)]), new(id, [new(Guid.Empty, 1)]),
            new(id, [new(id, null)]), new(id, [new(id, 0)]), new(id, [new(id, -1)]), new(id, [new(id, 1001)]),
            new(id, [new(id, 600), new(id, 401)]), new(id, Enumerable.Repeat<CheckoutItem?>(new(id, 1), 101).ToArray()),
            new(id, [new(id, 1), new(Guid.NewGuid(), 0)])];
        foreach (var request in requests)
        {
            var dependencies = new Dependencies();
            await Assert.ThrowsAsync<OrderingValidationException>(() => dependencies.Service.CreateAsync(request, default));
            Assert.Equal(0, dependencies.Calls);
            Assert.Null(dependencies.Saved);
        }
    }

    [Fact]
    public async Task Combined_quantity_is_checked_against_stock()
    {
        var dependencies = new Dependencies { Available = 3 };
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<OrderingConflictException>(() => dependencies.Service.CreateAsync(new(id, [new(id, 2), new(id, 2)]), default));
        Assert.Null(dependencies.Saved);
    }

    [Fact]
    public async Task Already_cancelled_request_never_contacts_dependencies_or_writes()
    {
        var dependencies = new Dependencies();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dependencies.Service.CreateAsync(
            new(Guid.NewGuid(), [new(Guid.NewGuid(), 1)]), cancelled.Token));
        Assert.Equal(0, dependencies.Calls);
        Assert.Null(dependencies.Saved);
    }
}
