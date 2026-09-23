using Ordering.Application;
using Ordering.Domain;
using Xunit;

namespace Ordering.Unit.Tests;

public sealed class OrderRulesTests
{
    private static readonly DateTime Created = new(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc);
    private static Order MakeOrder(params OrderItem[] items) => new(Guid.NewGuid(), Guid.NewGuid(), Created, items);
    private static OrderItem Item(int quantity = 1, decimal price = 10) => new(Guid.NewGuid(), "Product", price, quantity);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Invalid_quantity_is_rejected(int quantity) =>
        Assert.Throws<OrderingValidationException>(() => Item(quantity));

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.001")]
    [InlineData("100000000")]
    public void Invalid_price_is_rejected(string price) =>
        Assert.Throws<OrderingValidationException>(() => Item(price: decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Empty_ids_names_and_non_utc_creation_are_rejected()
    {
        Assert.Throws<OrderingValidationException>(() => new OrderItem(Guid.Empty, "Product", 1, 1));
        Assert.Throws<OrderingValidationException>(() => new OrderItem(Guid.NewGuid(), " ", 1, 1));
        Assert.Throws<OrderingValidationException>(() => new OrderItem(Guid.NewGuid(), new string('x', 121), 1, 1));
        Assert.Throws<OrderingValidationException>(() => new Order(Guid.Empty, Guid.NewGuid(), Created, [Item()]));
        Assert.Throws<OrderingValidationException>(() => new Order(Guid.NewGuid(), Guid.Empty, Created, [Item()]));
        Assert.Throws<OrderingValidationException>(() => new Order(Guid.NewGuid(), Guid.NewGuid(), DateTime.SpecifyKind(Created, DateTimeKind.Unspecified), [Item()]));
    }

    [Fact]
    public void Order_size_is_bounded()
    {
        Assert.Throws<OrderingValidationException>(() => MakeOrder());
        Assert.Throws<OrderingValidationException>(() => MakeOrder(Enumerable.Range(0, 101).Select(_ => Item()).ToArray()));
        Assert.Throws<OrderingValidationException>(() => new Order(Guid.NewGuid(), Guid.NewGuid(), Created, null));
    }

    [Fact]
    public void Totals_are_exact_and_snapshots_are_copied_into_read_only_items()
    {
        var id = Guid.NewGuid();
        var input = new[] { new OrderItem(id, "  Mouse  ", 0.10m, 3), Item(price: 0.20m) };
        var order = MakeOrder(input);
        input[0] = new OrderItem(id, "Later catalog name", 90m, 1);
        Assert.Equal(0.50m, order.Total);
        Assert.Equal("Mouse", order.Items[0].ProductName);
        Assert.Equal(0.30m, order.Items[0].LineTotal);
        Assert.All(order.Items, item => Assert.Equal(order.Id, item.OrderId));
        Assert.Throws<NotSupportedException>(() => ((IList<OrderItem>)order.Items).Clear());
        Assert.Equal("USD", order.Currency);
    }

    [Fact]
    public void Duplicate_products_merge_only_matching_snapshots_with_bounded_quantity()
    {
        var id = Guid.NewGuid();
        var order = MakeOrder(new(id, "Mouse", 29.99m, 2), new(id, "Mouse", 29.99m, 3));
        Assert.Equal(5, Assert.Single(order.Items).Quantity);
        Assert.Equal(149.95m, order.Total);
        Assert.Throws<OrderingValidationException>(() => MakeOrder(new(id, "Old", 1, 1), new(id, "New", 1, 1)));
        Assert.Throws<OrderingValidationException>(() => MakeOrder(new(id, "Mouse", 1, 1), new(id, "Mouse", 2, 1)));
        Assert.Throws<OrderingValidationException>(() => MakeOrder(new(id, "Mouse", 1, 600), new(id, "Mouse", 1, 401)));
    }

    [Fact]
    public void Maximum_order_and_zero_prices_do_not_overflow_or_round()
    {
        var order = MakeOrder(Enumerable.Range(0, 100).Select(_ => Item(1000, 99_999_999.99m)).ToArray());
        Assert.Equal(9_999_999_999_000m, order.Total);
        Assert.Equal(0, MakeOrder(Item(price: 0)).Total);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed, null)]
    [InlineData(OrderStatus.Rejected, "Insufficient stock")]
    [InlineData(OrderStatus.Cancelled, "Customer request")]
    public void Pending_can_reach_valid_outcomes(OrderStatus status, string? reason)
    {
        var order = MakeOrder(Item());
        Assert.Equal(OrderStatus.Pending, order.Status);
        order.TransitionTo(status, reason);
        Assert.Equal(status, order.Status);
        Assert.Equal(reason, order.StatusReason);
    }

    [Fact]
    public void Confirmed_can_cancel_but_cancelled_cannot_reconfirm()
    {
        var order = MakeOrder(Item());
        order.TransitionTo(OrderStatus.Confirmed);
        order.TransitionTo(OrderStatus.Cancelled);
        Assert.Throws<OrderTransitionException>(() => order.TransitionTo(OrderStatus.Confirmed));
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    public void Rejected_is_terminal(OrderStatus target)
    {
        var order = MakeOrder(Item());
        order.TransitionTo(OrderStatus.Rejected, "Original reason");
        Assert.Throws<OrderTransitionException>(() => order.TransitionTo(target));
        order.TransitionTo(OrderStatus.Rejected, "Replacement reason");
        Assert.Equal("Original reason", order.StatusReason);
    }

    [Fact]
    public void Invalid_transition_or_reason_preserves_state()
    {
        var order = MakeOrder(Item());
        Assert.Throws<OrderTransitionException>(() => order.TransitionTo(OrderStatus.Pending));
        Assert.Throws<OrderTransitionException>(() => order.TransitionTo((OrderStatus)42));
        Assert.Throws<OrderingValidationException>(() => order.TransitionTo(OrderStatus.Rejected, " "));
        Assert.Throws<OrderingValidationException>(() => order.TransitionTo(OrderStatus.Cancelled, new string('x', 501)));
        Assert.Throws<OrderingValidationException>(() => order.TransitionTo(OrderStatus.Confirmed, "A reason"));
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Null(order.StatusReason);
    }

    [Fact]
    public async Task Application_creates_pending_order_with_clock_and_cancellation()
    {
        var persistence = new FakePersistence();
        var clock = new FixedClock();
        using var cancellation = new CancellationTokenSource();
        var result = await new OrderingService(persistence, persistence, clock).CreateFromPricedLinesAsync(
            Guid.NewGuid(), [new(Guid.NewGuid(), "Product", 1.25m, 2)], cancellation.Token);
        Assert.Equal(Created, result.CreatedAtUtc);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(2.50m, result.Total);
        Assert.Equal(cancellation.Token, persistence.Token);
        Assert.NotNull(persistence.Saved);
    }

    [Fact]
    public async Task Invalid_creation_and_history_never_reach_persistence()
    {
        var persistence = new FakePersistence();
        var service = new OrderingService(persistence, persistence, new FixedClock());
        await Assert.ThrowsAsync<OrderingValidationException>(() => service.CreateFromPricedLinesAsync(Guid.NewGuid(), [], default));
        await Assert.ThrowsAsync<OrderingValidationException>(() => service.CreateFromPricedLinesAsync(Guid.Empty, [new(Guid.NewGuid(), "X", 1, 1)], default));
        await Assert.ThrowsAsync<OrderingValidationException>(() => service.ListForCustomerAsync(Guid.Empty, 1, 20, default));
        await Assert.ThrowsAsync<OrderingValidationException>(() => service.ListForCustomerAsync(Guid.NewGuid(), 0, 20, default));
        await Assert.ThrowsAsync<OrderingValidationException>(() => service.ListForCustomerAsync(Guid.NewGuid(), 1, 101, default));
        Assert.Equal(0, persistence.Reads);
        Assert.Null(persistence.Saved);
    }

    [Fact]
    public async Task Missing_detail_and_transition_return_application_not_found()
    {
        var persistence = new FakePersistence();
        var service = new OrderingService(persistence, persistence, new FixedClock());
        await Assert.ThrowsAsync<OrderingNotFoundException>(() => service.GetAsync(Guid.NewGuid(), default));
        await Assert.ThrowsAsync<OrderingNotFoundException>(() => service.TransitionAsync(Guid.NewGuid(), OrderStatus.Cancelled, null, default));
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Created);
    }
    private sealed class FakePersistence : IOrderingReader, IOrderingWriter
    {
        public Order? Saved { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Reads { get; private set; }
        public Task CreateAsync(Order order, CancellationToken ct)
        { Saved = order; Token = ct; return Task.CompletedTask; }
        public Task<bool> TransitionAsync(Guid id, OrderStatus target, string? reason, CancellationToken ct) => Task.FromResult(false);
        public Task<OrderDetailsDto?> GetAsync(Guid id, CancellationToken ct)
        { Reads++; return Task.FromResult<OrderDetailsDto?>(null); }
        public Task<OrderPage> ListForCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken ct)
        { Reads++; return Task.FromResult(new OrderPage([], 0, page, pageSize)); }
    }
}
