using BusinessManagement.Application.Reports;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

[Collection("postgres")]
public class ReportServiceTests(PostgresFixture fixture)
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private async Task<Guid> SeedBigCustomerAsync()           // doanh thu 30.000.000.000, 2 đơn
    {
        var customer = new Customer("Big", $"{Guid.NewGuid():N}@test.com");
        var product = new Product("Gold", "Cat", 10_000_000_000m, 100);
        var o1 = new Order(customer.Id); o1.AddItem(product, 2);
        var o2 = new Order(customer.Id); o2.AddItem(product, 1);

        await using var db = fixture.CreateContext();
        db.AddRange(customer, product, o1, o2);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    [Fact]
    public async Task NaivePerCustomerLoop_RunsOneQueryPerCustomer()        // ghi lại lỗi N+1
    {
        var ids = new List<Guid>();
        for (var i = 0; i < 5; i++) ids.Add(await SeedBigCustomerAsync());

        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(counter);

        var customers = await db.Customers.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync();
        foreach (var c in customers)
            await db.Orders.Where(o => o.CustomerId == c.Id)
                .SelectMany(o => o.Items).SumAsync(i => i.Quantity * i.UnitPrice);

        Assert.Equal(1 + 5, counter.Count);                                 // N + 1
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(50)]
    public async Task TopCustomers_RunsTwoQueriesRegardlessOfTop(int top)
    {
        await SeedBigCustomerAsync();
        var counter = new QueryCounter();
        await using var db = fixture.CreateContext(counter);

        await new ReportService(db).TopCustomersAsync(top, Ct);

        Assert.Equal(2, counter.Count);                                     // không phụ thuộc N
    }

    [Fact]
    public async Task TopCustomers_ReturnsCorrectRevenueAndOrderCount()
    {
        var customerId = await SeedBigCustomerAsync();
        await using var db = fixture.CreateContext();

        var result = await new ReportService(db).TopCustomersAsync(100, Ct);

        var row = Assert.Single(result, r => r.CustomerId == customerId);
        Assert.Equal(30_000_000_000m, row.Revenue);
        Assert.Equal(2, row.OrderCount);
    }
}