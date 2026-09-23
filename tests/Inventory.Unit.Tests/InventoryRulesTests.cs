using Inventory.Application;
using Inventory.Domain;
using Xunit;

namespace Inventory.Unit.Tests;

public sealed class InventoryRulesTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10, -1)]
    [InlineData(5, 6)]
    public void Invalid_quantities_are_rejected(int onHand, int reserved) =>
        Assert.Throws<InventoryValidationException>(() => new InventoryItem(Guid.NewGuid(), onHand, reserved));

    [Fact]
    public void Empty_product_id_is_rejected() =>
        Assert.Throws<InventoryValidationException>(() => new InventoryItem(Guid.Empty, 1));

    [Fact]
    public void Adjustments_change_on_hand_but_preserve_reserved()
    {
        var item = new InventoryItem(Guid.NewGuid(), 10, 4);
        item.AdjustOnHand(3);
        Assert.Equal(13, item.OnHand);
        Assert.Equal(9, item.Available);
        item.AdjustOnHand(-9);
        Assert.Equal(4, item.OnHand);
        Assert.Equal(4, item.Reserved);
        Assert.Equal(0, item.Available);
    }

    [Theory]
    [InlineData(-7)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Rejected_adjustment_preserves_state_and_cannot_overflow(int delta)
    {
        var item = new InventoryItem(Guid.NewGuid(), 10, 4);
        Assert.Throws<StockConflictException>(() => item.AdjustOnHand(delta));
        Assert.Equal(10, item.OnHand);
        Assert.Equal(4, item.Reserved);
        Assert.Equal(6, item.Available);
    }

    [Fact]
    public void Zero_delta_is_invalid_but_zero_and_maximum_stock_are_supported()
    {
        var item = new InventoryItem(Guid.NewGuid(), 0);
        Assert.Throws<InventoryValidationException>(() => item.AdjustOnHand(0));
        item.AdjustOnHand(int.MaxValue);
        Assert.Equal(int.MaxValue, item.Available);
        item.AdjustOnHand(-int.MaxValue);
        Assert.Equal(0, item.Available);
    }

    [Fact]
    public async Task Missing_initial_quantity_prevents_persistence()
    {
        var persistence = new FakePersistence();
        var service = new InventoryService(persistence, persistence);
        await Assert.ThrowsAsync<InventoryValidationException>(() =>
            service.CreateAsync(new(Guid.NewGuid(), null), default));
        Assert.Equal(0, persistence.Writes);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Invalid_pagination_never_queries_database(int page, int pageSize)
    {
        var persistence = new FakePersistence();
        await Assert.ThrowsAsync<InventoryValidationException>(() =>
            new InventoryService(persistence, persistence).ListAsync(page, pageSize, default));
        Assert.Equal(0, persistence.Reads);
    }

    [Fact]
    public async Task Adjustment_delegates_atomic_operation_and_cancellation_without_pre_read()
    {
        var persistence = new FakePersistence();
        using var cancellation = new CancellationTokenSource();
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<InventoryNotFoundException>(() =>
            new InventoryService(persistence, persistence).AdjustAsync(id, new(-1), cancellation.Token));
        Assert.Equal(id, persistence.ProductId);
        Assert.Equal(-1, persistence.Delta);
        Assert.Equal(cancellation.Token, persistence.Token);
        Assert.Equal(0, persistence.Reads);
        Assert.Equal(1, persistence.Writes);
    }

    private sealed class FakePersistence : IInventoryReader, IInventoryWriter
    {
        public int Reads { get; private set; }
        public int Writes { get; private set; }
        public Guid ProductId { get; private set; }
        public int Delta { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<InventoryItemDto?> GetAsync(Guid productId, CancellationToken ct)
        { Reads++; return Task.FromResult<InventoryItemDto?>(null); }
        public Task<InventoryPage> ListAsync(int page, int pageSize, CancellationToken ct)
        { Reads++; return Task.FromResult(new InventoryPage([], 0, page, pageSize)); }
        public Task CreateAsync(InventoryItem item, CancellationToken ct)
        { Writes++; return Task.CompletedTask; }
        public Task<InventoryItemDto?> AdjustAsync(Guid productId, int delta, CancellationToken ct)
        {
            Writes++; ProductId = productId; Delta = delta; Token = ct;
            return Task.FromResult<InventoryItemDto?>(null);
        }
    }
}
