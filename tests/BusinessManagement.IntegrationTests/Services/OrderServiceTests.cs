using BusinessManagement.Application.Common;
using BusinessManagement.Application.Orders;
using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;
using BusinessManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

[Collection("postgres")]
public class OrderServiceTests(PostgresFixture fixture)
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private async Task<(Guid CustomerId, Guid[] Products)> SeedAsync(params int[] stocks)
    {
        var customer = new Customer("Buyer", $"{Guid.NewGuid():N}@test.com");
        var products = stocks.Select((s, i) => new Product($"P{i}-{Guid.NewGuid():N}", "Cat", 10m, s)).ToList();
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
        return await new OrderService(db).CreateAsync(customerId, lines, Ct);
    }

    [Fact]
    public async Task Create_ReservesStockAndReturnsTotal()
    {
        var (customerId, p) = await SeedAsync(10);

        var order = await CreateAsync(customerId, new OrderLine(p[0], 3));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(30m, order.Total);
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
            Assert.Equal(OrderStatus.Cancelled, (await new OrderService(db).CancelAsync(order.Id, Ct)).Status);
        Assert.Equal(10, await StockAsync(p[0]));

        await using var db2 = fixture.CreateContext();
        await Assert.ThrowsAsync<DomainException>(() => new OrderService(db2).CancelAsync(order.Id, Ct));
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
                await new OrderService(db).CancelAsync(order.Id, Ct);
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
}