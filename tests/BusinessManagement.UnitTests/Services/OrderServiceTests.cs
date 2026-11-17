using System.Net;
using System.Text.Json;
using BusinessManagement.Application.Checkout.Discount;
using BusinessManagement.Application.Checkout.Pricing;
using BusinessManagement.Application.Common;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Application.Orders;
using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;
using BusinessManagement.Infrastructure.Persistence;
using BusinessManagement.UnitTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[Collection("postgres")]
public class OrderServiceTests(PostgresFixture fixture)
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static OrderService NewSut(AppDbContext db, ILogger<OrderService>? logger = null, IEventBus? events = null)
        => new(db,
               new PricingService(
                   new DiscountCalculator(Array.Empty<IDiscountStrategy>()),
                   new InMemoryVoucherFactory()),
               logger ?? NullLogger<OrderService>.Instance,
               events ?? new FakeEventBus());
    private async Task<(Guid CustomerId, Guid[] Products)> SeedAsync(params int[] stocks)
    {
        var customer = new Customer("Buyer", $"{Guid.NewGuid():N}@test.com");
        var products = stocks.Select((s, i) => new Product($"P{i}-{Guid.NewGuid():N}", "Cat", 100m, s)).ToList();
        await using var db = fixture.CreateContext();
        db.Customers.Add(customer);
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
        return (customer.Id, products.Select(p => p.Id).ToArray());
    }
    private async Task<(Guid CustomerId, Guid[] Products)> SeedWithPriceAsync(decimal price, params int[] stocks)
    {
        var customer = new Customer("Buyer", $"{Guid.NewGuid():N}@test.com");
        var products = stocks.Select((s, i) => new Product($"P{i}-{Guid.NewGuid():N}", "Cat", price, s)).ToList();
        await using var db = fixture.CreateContext();
        db.Customers.Add(customer);
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
        return (customer.Id, products.Select(p => p.Id).ToArray());
    }

    private async Task<int> StockAsync(Guid productId)
    {
        await using var db = fixture.CreateContext();
        return await db.Products.AsNoTracking().Where(p => p.Id == productId).Select(p => p.Stock).SingleAsync();
    }

    private async Task<OrderDto> CreateAsync(Guid customerId, params OrderLine[] lines)
    {
        await using var db = fixture.CreateContext();
        return await NewSut(db).CreateAsync(customerId, lines, null, Ct);
    }
    private async Task<OrderDto> CreateWithVoucherAsync(Guid customerId, string? voucher, params OrderLine[] lines)
    {
        await using var db = fixture.CreateContext();
        return await NewSut(db).CreateAsync(customerId, lines, voucher, Ct);
    }

    [Fact]
    public async Task Create_ReservesStockAndReturnsTotal()
    {
        var (customerId, p) = await SeedAsync(10);

        var order = await CreateAsync(customerId, new OrderLine(p[0], 3));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(300m, order.Total);
        Assert.Equal(7, await StockAsync(p[0]));
    }

    [Fact]
    public async Task Create_DuplicateLines_AreMergedIntoOne()
    {
        var (customerId, p) = await SeedAsync(10);

        var order = await CreateAsync(customerId, new OrderLine(p[0], 2), new OrderLine(p[0], 3));

        Assert.Equal(5, Assert.Single(order.Items).Quantity);
        Assert.Equal(5, await StockAsync(p[0]));
    }

    [Fact]
    public async Task Create_LastLineOutOfStock_RollsBackEarlierLinesAndCreatesNoOrder()
    {
        var (customerId, p) = await SeedAsync(10, 10);
        var (first, last) = (p.Min(), p.Max());
        await using (var db = fixture.CreateContext())
            await db.Products.Where(x => x.Id == last)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, 1));

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateAsync(customerId, new OrderLine(first, 2), new OrderLine(last, 5)));

        Assert.Equal(10, await StockAsync(first));
        Assert.Equal(1, await StockAsync(last));
        await using var check = fixture.CreateContext();
        Assert.False(await check.Orders.AnyAsync(o => o.CustomerId == customerId));
    }

    [Fact]
    public async Task Create_TenConcurrentOrdersForFiveUnits_SellsExactlyFive()
    {
        var (customerId, p) = await SeedAsync(5);

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            try { await CreateAsync(customerId, new OrderLine(p[0], 1)); return true; }
            catch (ConflictException) { return false; }
        }));

        Assert.Equal(5, results.Count(r => r));
        Assert.Equal(0, await StockAsync(p[0]));
        await using var db = fixture.CreateContext();
        Assert.Equal(5, await db.Orders.CountAsync(o => o.CustomerId == customerId));
    }

    [Fact]
    public async Task Create_ConcurrentOrdersWithReversedLineOrder_NeverDeadlocksOrOversells()
    {
        var (customerId, p) = await SeedAsync(10, 10);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async i =>
        {
            var lines = i % 2 == 0
                ? new[] { new OrderLine(p[0], 1), new OrderLine(p[1], 1) }
                : new[] { new OrderLine(p[1], 1), new OrderLine(p[0], 1) };
            try { await CreateAsync(customerId, lines); return true; }
            catch (ConflictException) { return false; }
        }));

        Assert.Equal(10, results.Count(r => r));
        Assert.Equal(0, await StockAsync(p[0]));
        Assert.Equal(0, await StockAsync(p[1]));
    }

    [Fact]
    public async Task Cancel_RestoresStock_AndSecondCancelThrows()
    {
        var (customerId, p) = await SeedAsync(10);
        var order = await CreateAsync(customerId, new OrderLine(p[0], 4));

        await using (var db = fixture.CreateContext())
            Assert.Equal(OrderStatus.Cancelled, (await NewSut(db).CancelAsync(order.Id, Ct)).Status);
        Assert.Equal(10, await StockAsync(p[0]));

        await using var db2 = fixture.CreateContext();
        await Assert.ThrowsAsync<DomainException>(() => NewSut(db2).CancelAsync(order.Id, Ct));
        Assert.Equal(10, await StockAsync(p[0]));
    }

    [Fact]
    public async Task Cancel_TenConcurrentCalls_RestoresStockExactlyOnce()
    {
        var (customerId, p) = await SeedAsync(10);
        var order = await CreateAsync(customerId, new OrderLine(p[0], 4));

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            try
            {
                await using var db = fixture.CreateContext();
                await NewSut(db).CancelAsync(order.Id, Ct);
                return true;
            }
            catch (DomainException) { return false; }
        }));

        Assert.Equal(1, results.Count(r => r));
        Assert.Equal(10, await StockAsync(p[0]));
    }

    [Fact]
    public async Task NaiveReadModifyWrite_LosesUpdates()
    {
        var (_, p) = await SeedAsync(5);
        var contexts = new List<AppDbContext>();
        var loaded = new List<Product>();
        for (var i = 0; i < 10; i++)
        {
            var db = fixture.CreateContext();
            contexts.Add(db);
            loaded.Add(await db.Products.SingleAsync(x => x.Id == p[0]));
        }
        for (var i = 0; i < 10; i++)
        {
            loaded[i].DecreaseStock(1);
            await contexts[i].SaveChangesAsync();
        }
        contexts.ForEach(c => c.Dispose());

        Assert.Equal(4, await StockAsync(p[0]));
    }

    [Fact]
    public async Task Create_CustomerNotFound_ThrowsDomainException_AndStockUnchanged()
    {
        var (_, p) = await SeedAsync(10);
        var nonExistentCustomerId = Guid.NewGuid();

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateAsync(nonExistentCustomerId, new OrderLine(p[0], 3)));

        Assert.Equal(10, await StockAsync(p[0]));

        await using var db = fixture.CreateContext();
        Assert.Empty(await db.Orders.Where(o => o.CustomerId == nonExistentCustomerId).ToListAsync());
    }

    [Fact]
    public async Task Create_DeactivatedCustomer_ThrowsDomainException_AndStockUnchanged()
    {
        var (customerId, p) = await SeedAsync(10);

        await using (var db = fixture.CreateContext())
        {
            var customer = await db.Customers.SingleAsync(x => x.Id == customerId);
            customer.Deactivate();

            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateAsync(customerId, new OrderLine(p[0], 3)));

        Assert.Equal(10, await StockAsync(p[0]));

        await using var check = fixture.CreateContext();
        Assert.False(await check.Orders.AnyAsync(c => c.CustomerId == customerId));
    }
    [Fact]
    public async Task Create_ProductNotFound_ThrowsDomainException_AndRollsBackStock()
    {
        var (customerId, p) = await SeedAsync(10, 10);

        var first = p.Min();
        var nonExistentProductId = Guid.NewGuid();

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateAsync(
                customerId,
                new OrderLine(first, 3),
                new OrderLine(nonExistentProductId, 2)));

        Assert.Equal(10, await StockAsync(first));

        await using var check = fixture.CreateContext();
        Assert.Empty(await check.Orders.Where(o => o.CustomerId == customerId).ToListAsync());
    }

    [Fact]
    public async Task Create_Voucher50k_ReturnsTotal50k()
    {
        var (customerId, p) = await SeedAsync(1);

        var order = await CreateWithVoucherAsync(customerId, "SALE50K", new OrderLine(p[0], 1));

        Assert.Equal(50m, order.Total);
    }

    [Fact]
    public async Task Create_Voucher10k_ReturnsTotal90k()
    {
        var (customerId, p) = await SeedAsync(1);

        var order = await CreateWithVoucherAsync(customerId, "SALE10", new OrderLine(p[0], 1));

        Assert.Equal(90m, order.Total);
    }

    [Fact]
    public async Task Create_VoucherGreaterThanOrder_DiscountCappedAtOrderTotal()
    {
        var (customerId, p) = await SeedWithPriceAsync(30m, 1);

        var order = await CreateWithVoucherAsync(
            customerId,
            "SALE50K",
            new OrderLine(p[0], 1));

        // Assert
        Assert.Equal(30m, order.Discount);
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public async Task Create_InvalidVoucher_ThrowsDomainException_AndDoesNotChangeStock()
    {
        var (customerId, p) = await SeedAsync(10);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateWithVoucherAsync(
                customerId,
                "NOPE",
                new OrderLine(p[0], 3)));

        Assert.Equal(10, await StockAsync(p[0]));

        await using var db = fixture.CreateContext();

        Assert.False(await db.Orders.AnyAsync(o => o.CustomerId == customerId));
    }
    [Fact]
    public async Task Create_Success_LogsOrderIdAtInformationLevel()
    {
        var (customerId, p) = await SeedAsync(10);

        var logger = new ListLogger<OrderService>();

        await using var db = fixture.CreateContext();

        var order = await NewSut(db, logger).CreateAsync(
            customerId,
            new[] { new OrderLine(p[0], 2) },
            null,
            Ct);

        var entry = Assert.Single(
            logger.Entries, x => x.Level == LogLevel.Information);

        Assert.Equal(order.Id, entry.Values["OrderId"]);
    }

    [Fact]
    public async Task Create_OutOfStock_LogsProductIdAtWarningLevel()
    {
        var (customerId, p) = await SeedAsync(0);

        var logger = new ListLogger<OrderService>();

        await using var db = fixture.CreateContext();

        await Assert.ThrowsAsync<ConflictException>(() =>
            NewSut(db, logger).CreateAsync(
                customerId,
                new[] { new OrderLine(p[0], 1) },
                null,
                Ct));

        var entry = Assert.Single(
            logger.Entries, x => x.Level == LogLevel.Warning);

        Assert.Equal(p[0], entry.Values["ProductId"]);

        Assert.Equal(0, await StockAsync(p[0]));
    }
    [Fact]
    public async Task List_FilterByCustomer_ReturnsOnlyCustomerOrders()
    {
        var (customer1, p1) = await SeedAsync(10);
        var (customer2, p2) = await SeedAsync(10);

        await CreateAsync(customer1, new OrderLine(p1[0], 1));
        await CreateAsync(customer1, new OrderLine(p1[0], 2));
        await CreateAsync(customer2, new OrderLine(p2[0], 1));

        await using var db = fixture.CreateContext();

        var result = await NewSut(db).ListAsync(
            new OrderListQuery(customer1, null, null, null, null),
            Ct);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Equal(customer1, x.CustomerId));
    }
    [Fact]
    public async Task List_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        var (customerId, p) = await SeedAsync(10);

        var pending = await CreateAsync(customerId, new OrderLine(p[0], 1));
        var cancelled = await CreateAsync(customerId, new OrderLine(p[0], 1));

        await using (var db = fixture.CreateContext())
            await NewSut(db).CancelAsync(cancelled.Id, Ct);

        await using var check = fixture.CreateContext();

        var result = await NewSut(check).ListAsync(
            new OrderListQuery(customerId, OrderStatus.Pending, null, null, null),
            Ct);

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task List_FilterByDateRange_ReturnsOrdersWithinRange()
    {
        var (customerId, p) = await SeedAsync(10);

        var order1 = await CreateAsync(customerId, new OrderLine(p[0], 1));
        var order2 = await CreateAsync(customerId, new OrderLine(p[0], 1));
        var order3 = await CreateAsync(customerId, new OrderLine(p[0], 1));

        var from = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = fixture.CreateContext())
        {
            await db.Orders.Where(o => o.Id == order1.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.CreatedAt, from.AddDays(-1)));

            await db.Orders.Where(o => o.Id == order2.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.CreatedAt, from));

            await db.Orders.Where(o => o.Id == order3.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.CreatedAt, to));
        }

        await using var check = fixture.CreateContext();

        var result = await NewSut(check).ListAsync(
            new OrderListQuery(customerId, null, from, to, null),
            Ct);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(order2.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task List_Pagination_DoesNotDuplicateOrSkipOrders()
    {
        var (customerId, p) = await SeedAsync(20);

        for (var i = 0; i < 5; i++)
            await CreateAsync(customerId, new OrderLine(p[0], 1));

        await using var db = fixture.CreateContext();
        var sut = NewSut(db);

        var page1 = await sut.ListAsync(
            new OrderListQuery(customerId, null, null, null, null, 1, 2), Ct);

        var page2 = await sut.ListAsync(
            new OrderListQuery(customerId, null, null, null, null, 2, 2), Ct);

        var page3 = await sut.ListAsync(
            new OrderListQuery(customerId, null, null, null, null, 3, 2), Ct);

        var allIds = page1.Items
            .Concat(page2.Items)
            .Concat(page3.Items)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(5, allIds.Count);
        Assert.Equal(5, allIds.Distinct().Count());
        Assert.Equal(3, page1.TotalPage);
        Assert.Equal(3, page2.TotalPage);
        Assert.Equal(3, page3.TotalPage);
    }
    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    public async Task List_ExecutesExactlyTwoQueries(int pageSize)
    {
        var (customerId, p) = await SeedAsync(100);

        for (var i = 0; i < 10; i++)
            await CreateAsync(customerId, new OrderLine(p[0], 1));

        var counter = new QueryCounter();

        await using var db = fixture.CreateContext(counter);
        var sut = NewSut(db);

        var result = await sut.ListAsync(
            new OrderListQuery(
                customerId, null, null, null, null, 1, pageSize),
            Ct);

        Assert.Equal(2, counter.Count);
        Assert.Equal(10, result.TotalCount);
    }
    [Fact]
    public async Task Create_LogsDoNotContainCustomerEmail()
    {
        var (customerId, p) = await SeedAsync(10);

        await using var db = fixture.CreateContext();

        var customer = await db.Customers
            .SingleAsync(c => c.Id == customerId);

        var email = customer.Email;

        var logger = new ListLogger<OrderService>();

        await NewSut(db, logger).CreateAsync(
            customerId,
            new[] { new OrderLine(p[0], 1) },
            null,
            Ct);

        Assert.DoesNotContain(
            logger.Entries,
            entry => entry.Message.Contains(
                email,
                StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task Create_Success_PublishesOrderCreatedEvent()
    {
        var (customerId, p) = await SeedAsync(10);

        await using var db = fixture.CreateContext();

        var events = new FakeEventBus();
        var logger = new ListLogger<OrderService>();

        var order = await NewSut(db, logger, events).CreateAsync(
            customerId,
            new[] { new OrderLine(p[0], 2) },
            null,
            Ct);

        var published = Assert.Single(events.Published);

        Assert.Equal(RoutingKeys.OrderCreated, published.RoutingKey);

        var message = Assert.IsType<OrderCreated>(published.Message);

        Assert.NotEqual(Guid.Empty, message.EventId);
        Assert.Equal(order.Id, message.OrderId);
        Assert.Equal(customerId, message.CustomerId);
        Assert.Equal(order.Total, message.Total);
    }
    [Fact]
    public async Task Create_OutOfStock_DoesNotPublishEvent()
    {
        var (customerId, p) = await SeedAsync(0);

        await using var db = fixture.CreateContext();

        var events = new FakeEventBus();
        var logger = new ListLogger<OrderService>();

        await Assert.ThrowsAsync<ConflictException>(() =>
            NewSut(db, logger, events).CreateAsync(
                customerId,
                new[] { new OrderLine(p[0], 1) },
                null,
                Ct));

        Assert.Empty(events.Published);
    }
    [Fact]
    public async Task Create_InvalidVoucher_DoesNotPublishEvent()
    {
        var (customerId, p) = await SeedAsync(10);

        await using var db = fixture.CreateContext();

        var events = new FakeEventBus();
        var logger = new ListLogger<OrderService>();

        await Assert.ThrowsAsync<DomainException>(() =>
            NewSut(db, logger, events).CreateAsync(
                customerId,
                new[] { new OrderLine(p[0], 1) },
                "INVALID",
                Ct));

        Assert.Empty(events.Published);
    }
    [Fact]
    public async Task Create_InactiveCustomer_DoesNotPublishEvent()
    {
        var (customerId, p) = await SeedAsync(10);

        await using var db = fixture.CreateContext();

        var customer = await db.Customers.SingleAsync(
            c => c.Id == customerId);

        customer.Deactivate();
        await db.SaveChangesAsync();

        var events = new FakeEventBus();
        var logger = new ListLogger<OrderService>();

        await Assert.ThrowsAsync<DomainException>(() =>
            NewSut(db, logger, events).CreateAsync(
                customerId,
                new[] { new OrderLine(p[0], 1) },
                null,
                Ct));

        Assert.Empty(events.Published);
    }
    [Fact]
    public async Task Create_EventBusThrows_OrderIsStillPersistedAndErrorIsLogged()
    {
        var (customerId, p) = await SeedAsync(10);

        var events = new FakeEventBus
        {
            ThrowOnPublish = new InvalidOperationException("RabbitMQ unavailable")
        };

        var logger = new ListLogger<OrderService>();

        Guid orderId;

        await using (var db = fixture.CreateContext())
        {
            var order = await NewSut(db, logger, events).CreateAsync(
                customerId,
                new[] { new OrderLine(p[0], 1) },
                null,
                Ct);

            orderId = order.Id;
        }

        // Dùng DbContext mới để xác nhận dữ liệu thực sự đã được lưu.
        await using (var verifyDb = fixture.CreateContext())
        {
            Assert.True(await verifyDb.Orders.AnyAsync(o => o.Id == orderId));
        }

        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains(orderId.ToString()));
    }
    [Fact]
    public async Task Create_OrderCreatedEventDoesNotContainCustomerEmail()
    {
        var (customerId, p) = await SeedAsync(10);

        await using var db = fixture.CreateContext();

        var customer = await db.Customers.SingleAsync(
            c => c.Id == customerId);

        var email = customer.Email;

        var events = new FakeEventBus();
        var logger = new ListLogger<OrderService>();

        await NewSut(db, logger, events).CreateAsync(
            customerId,
            new[] { new OrderLine(p[0], 1) },
            null,
            Ct);

        var published = Assert.Single(events.Published);

        var json = JsonSerializer.Serialize(published.Message);

        Assert.DoesNotContain(
            email,
            json,
            StringComparison.OrdinalIgnoreCase);
    }
}