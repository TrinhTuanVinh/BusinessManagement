using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Collection("postgres")]
public class PersistenceTests(PostgresFixture fixture)
{
    private static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    [Fact]
    public async Task Product_RoundTrip_PreservesValues()
    {
        var product = new Product("Wood 1", "Wood", 19.02m, 5);
        await using (var db = fixture.CreateContext())
        {
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        await using var db2 = fixture.CreateContext();
        var loaded = await db2.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);

        Assert.Equal("Wood 1", loaded.Name);
        Assert.Equal(19.02m, loaded.Price);
        Assert.Equal(5, loaded.Stock);
    }

    [Fact]
    public async Task Customer_DuplicateEmail_ViolatesUniqueIndex()
    {
        var email = NewEmail();
        await using var db = fixture.CreateContext();
        db.Customers.Add(new Customer("A", email));
        await db.SaveChangesAsync();

        await using var db2 = fixture.CreateContext();
        db2.Customers.Add(new Customer("B", email));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task Product_NegativeStockBypassingDomain_IsRejectedByCheckConstraint()
    {
        var product = new Product("Plastic 1", "Plastic", 10m, 5);
        await using var db = fixture.CreateContext();
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.Entry(product).Property(p => p.Stock).CurrentValue = -1;   // giả lập bug bỏ qua Domain
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, pg.SqlState);
    }

    [Fact]
    public async Task Order_WithItems_RoundTripsThroughBackingField()
    {
        var customer = new Customer("C", NewEmail());
        var p1 = new Product("P1", "Cat", 19m, 10);
        var p2 = new Product("P2", "Cat", 10m, 10);
        var order = new Order(customer.Id);
        order.AddItem(p1, 2);
        order.AddItem(p2, 1);

        await using (var db = fixture.CreateContext())
        {
            db.AddRange(customer, p1, p2);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        await using var db2 = fixture.CreateContext();
        var loaded = await db2.Orders.Include(o => o.Items).AsNoTracking()
            .SingleAsync(o => o.Id == order.Id);

        Assert.Equal(2, loaded.Items.Count);
        Assert.Equal(48m, loaded.Total);                      // 2*19 + 1*10
        Assert.Equal(OrderStatus.Pending, loaded.Status);
    }

    [Fact]
    public async Task Customer_WithOrders_CannotBeDeleted()
    {
        var customer = new Customer("D", NewEmail());
        var order = new Order(customer.Id);
        await using var db = fixture.CreateContext();
        db.Customers.Add(customer);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await using var db2 = fixture.CreateContext();
        db2.Customers.Remove(await db2.Customers.SingleAsync(c => c.Id == customer.Id));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.RestrictViolation, ((PostgresException)ex.InnerException!).SqlState);
    }
    
    [Fact]
    public async Task OrderItem_WithoutOrder_IsRejectedByNotNull()
    {
        var product = new Product("P-orphan", "Cat", 10m, 5);
        await using var db = fixture.CreateContext();
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync(
                $"""
            INSERT INTO "OrderItems" ("Id", "ProductId", "Quantity", "UnitPrice", "CreatedAt")
            VALUES ({Guid.NewGuid()}, {product.Id}, 1, 10, NOW())
            """));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, ex.SqlState);
    }
}