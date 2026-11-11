using BusinessManagement.Application.Common;
using BusinessManagement.Application.Products;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Collection("postgres")]
public class ProductServiceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task List_SamePrice_PagesDoNotOverlapAndCoverEverything()
    {
        var category = $"cat-{Guid.NewGuid():N}";
        await using var db = fixture.CreateContext();
        for (var i = 0; i < 25; i++) db.Products.Add(new Product($"P{i}", category, 10m, 1));   // cùng giá
        await db.SaveChangesAsync();
        var sut = new ProductService(db);

        var seen = new HashSet<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await sut.ListAsync(
                new ProductListQuery(null, category, null, null, null, "price", page, 10), CancellationToken.None);

            Assert.Equal(25, result.TotalCount);
            foreach (var item in result.Items) Assert.True(seen.Add(item.Id));   // không trang nào lặp dòng
        }
        Assert.Equal(25, seen.Count);                                            // không bỏ sót dòng nào
    }

    [Fact]
    public async Task Get_UnknownId_ThrowsNotFound()
    {
        await using var db = fixture.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(
            () => new ProductService(db).GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private static readonly CancellationToken Ct = CancellationToken.None;
    [Fact]
    public async Task Create_ThenGet_ReturnsSameValues()
    {
        await using var db = fixture.CreateContext();
        var created = await new ProductService(db).CreateAsync("Physic 1", "Physic", 100m, 10, Ct);

        await using var db2 = fixture.CreateContext();
        var loaded = await new ProductService(db2).GetAsync(created.Id, Ct);

        Assert.Equal("Physic 1", loaded.Name);
        Assert.Equal(100m, loaded.Price);
        Assert.Equal(10, loaded.Stock);
        Assert.InRange(loaded.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        Assert.Null(loaded.UpdatedAt);
    }

    [Fact]
    public async Task Update_ChangesDetailsKeepsStockAndSetsUpdatedAt()
    {
        await using var db = fixture.CreateContext();
        var created = await new ProductService(db).CreateAsync("Laptop", "HP", 200m, 2, Ct);

        await using var db2 = fixture.CreateContext();
        await new ProductService(db2).UpdateAsync(created.Id, "Phone", "Iphone", 150m, Ct);

        await using var db3 = fixture.CreateContext();
        var loaded = await new ProductService(db3).GetAsync(created.Id, Ct);
        Assert.Equal("Phone", loaded.Name);
        Assert.Equal("Iphone", loaded.Category);
        Assert.Equal(150m, loaded.Price);
        Assert.Equal(2, loaded.Stock);
        Assert.NotNull(loaded.UpdatedAt);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ThenGetThrowsNotFound()
    {
        await using var db = fixture.CreateContext();
        var created = await new ProductService(db).CreateAsync("Temp", "Cat", 10m, 1, Ct);

        await using var db2 = fixture.CreateContext();
        await new ProductService(db2).DeleteAsync(created.Id, Ct);

        await using var db3 = fixture.CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(() => new ProductService(db3).GetAsync(created.Id, Ct));
    }

    [Fact]
    public async Task Delete_UnknownId_ThrowsNotFound()
    {
        await using var db = fixture.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() => new ProductService(db).DeleteAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task Delete_ProductInsideAnOrder_FailsWithIntegrityViolation()
    {
        var customer = new Customer("C", $"{Guid.NewGuid():N}@test.com");
        var p1 = new Product("P1", "Cat", 19m, 10);
        var order = new Order(customer.Id);
        order.AddItem(p1, 2);

        await using (var seed = fixture.CreateContext())
        {
            seed.AddRange(customer, p1);
            seed.Orders.Add(order);
            await seed.SaveChangesAsync();
        }

        await using var db = fixture.CreateContext();
        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => new ProductService(db).DeleteAsync(p1.Id, Ct));

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.StartsWith("23", pg.SqlState);                 // RESTRICT được báo bằng 23001
    }

    [Fact]
    public async Task List_FilterByPriceRange_SortedByPriceDescending()
    {
        var category = $"cat-{Guid.NewGuid():N}";
        await using var db = fixture.CreateContext();
        foreach (var price in new[] { 5m, 15m, 25m, 35m })
            db.Products.Add(new Product($"Wood {price}", category, price, 1));
        await db.SaveChangesAsync();

        var result = await new ProductService(db).ListAsync(
            new ProductListQuery(null, category, 10m, 30m, null, "-price"), Ct);

        Assert.Equal(new[] { 25m, 15m }, result.Items.Select(i => i.Price));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task List_PageSizeAboveLimit_IsClampedTo100()
    {
        var category = $"cat-{Guid.NewGuid():N}";
        await using var db = fixture.CreateContext();
        var random = new Random();
        for (var i = 0; i < 101; i++) db.Products.Add(new Product($"P{i}", category, 10m, 1));   // 101 > giới hạn
        await db.SaveChangesAsync();

        var result = await new ProductService(db).ListAsync(
            new ProductListQuery(null, category, null, null, null, null, 1, 1000), Ct);

        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, result.Items.Count);
        Assert.Equal(101, result.TotalCount);
        Assert.Equal(2, result.TotalPage);
    }

    [Fact]
    public async Task List_SearchWood_ReturnProductWood1()
    {
        var category = $"cat-{Guid.NewGuid():N}";
        await using var db = fixture.CreateContext();
        db.Products.AddRange(
            new Product("Wood 1", category, 1m, 1),
            new Product("WOODEN chair", category, 2m, 1),
            new Product("Plastic 1", category, 3m, 1));
        await db.SaveChangesAsync();
        var sut = new ProductService(db);

        var wood = await sut.ListAsync(new ProductListQuery("wOoD", category, null, null, null, null), Ct);
        var none = await sut.ListAsync(new ProductListQuery("zzz", category, null, null, null, null), Ct);

        Assert.Equal(2, wood.TotalCount);
        Assert.DoesNotContain(wood.Items, i => i.Name == "Plastic 1");
        Assert.Equal(0, none.TotalCount);
    }
}