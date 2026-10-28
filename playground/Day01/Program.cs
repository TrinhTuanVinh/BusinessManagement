using System.Diagnostics;

List<Product> products = new List<Product>();
products.Add(new Product(1, "Plastic 1", "Plastic", 19.02M, 1));
products.Add(new Product(2, "Plastic 2", "Plastic", 15.2M, 3));
products.Add(new Product(3, "Plastic 3", "Plastic", 29.02M, 4));
products.Add(new Product(4, "Plastic 4", "Plastic", 35M, 9));
products.Add(new Product(5, "Wood 1", "Wood", 19M, 5));
products.Add(new Product(6, "Wood 2", "Wood", 12M, 6));
products.Add(new Product(7, "Wood 3", "Wood", 102M, 7));
products.Add(new Product(8, "Wood 4", "Wood", 10M, 2));
List<Customer> customers = new List<Customer>(){new Customer ( 1, "Vinh"),
                                                new Customer ( 2, "Nga"),
                                                new Customer ( 3, "Thanh"),
                                                new Customer ( 4, "Vy"),
                                                new Customer ( 5, "Minh")};
List<Order> orders = new List<Order>()
{
    new Order(1, 1, DateTime.UtcNow.AddDays(-10), "Order 1 - Plastic Items", new List<OrderItem>()
    {
        new OrderItem(1, 2, 19.02M),
        new OrderItem(2, 3, 15.20M)
    }),

    new Order(2, 2, DateTime.UtcNow.AddDays(-9), "Order 2 - Wood Items", new List<OrderItem>()
    {
        new OrderItem(5, 2, 19M),
        new OrderItem(6, 1, 12M)
    }),

    new Order(3, 3, DateTime.UtcNow.AddDays(-8), "Order 3 - Mixed Items", new List<OrderItem>()
    {
        new OrderItem(3, 1, 29.02M),
        new OrderItem(8, 4, 10M)
    }),

    new Order(4, 1, DateTime.UtcNow.AddDays(-7), "Order 4 - Large Plastic Order", new List<OrderItem>()
    {
        new OrderItem(4, 3, 35M),
        new OrderItem(1, 5, 19.02M)
    }),

    new Order(5, 4, DateTime.UtcNow.AddDays(-6), "Order 5 - Wood Furniture", new List<OrderItem>()
    {
        new OrderItem(7, 2, 102M),
        new OrderItem(5, 1, 19M)
    }),

    new Order(6, 5, DateTime.UtcNow.AddDays(-5), "Order 6 - Small Order", new List<OrderItem>()
    {
        new OrderItem(8, 1, 10M),
        new OrderItem(2, 1, 15.20M)
    }),

    new Order(7, 2, DateTime.UtcNow.AddDays(-4), "Order 7 - Plastic Bulk", new List<OrderItem>()
    {
        new OrderItem(1, 10, 19.02M),
        new OrderItem(3, 5, 29.02M)
    }),

    new Order(8, 3, DateTime.UtcNow.AddDays(-3), "Order 8 - Wood Bulk", new List<OrderItem>()
    {
        new OrderItem(6, 4, 12M),
        new OrderItem(7, 1, 102M),
        new OrderItem(8, 6, 10M)
    }),

    new Order(9, 4, DateTime.UtcNow.AddDays(-2), "Order 9 - Premium Items", new List<OrderItem>()
    {
        new OrderItem(7, 3, 102M),
        new OrderItem(4, 2, 35M)
    }),

    new Order(10, 5, DateTime.UtcNow.AddDays(-1), "Order 10 - Mixed Bulk Order", new List<OrderItem>()
    {
        new OrderItem(2, 4, 15.20M),
        new OrderItem(5, 3, 19M),
        new OrderItem(8, 2, 10M)
    })
};
var date = DateTime.UtcNow;
var o1 = new Order(1, 1, date, null, new List<OrderItem>());
var o2 = new Order(1, 1, date, null, new List<OrderItem>());
Console.WriteLine(o1 == o2);
customers.Add(new Customer(6, "An"));                              // khách chưa có đơn
orders.Add(new Order(11, 1, DateTime.UtcNow, null, new List<OrderItem>())); // đơn rỗng, Note = null
orders.Add(new Order(12, 1, DateTime.UtcNow, "Extra", new List<OrderItem> { new(1, 1, 19.02M) })); // khách 1 đủ 3 đơn
var a = new Customer(1, "Vinh");
var b = new Customer(1, "Vinh");
Console.WriteLine(a == b);              // True (record)
Console.WriteLine(ReferenceEquals(a, b)); // False, vẫn là 2 object khác nhau
var c = a with { Name = "Nga" };        // tạo bản sao đã sửa
var list1 = products
            .Where(p => p.Stock < 10)
            .OrderBy(p => p.Stock)
            .Select(p => new { p.Name, p.Price, p.Stock })
            .ToList();
Console.WriteLine("list 1");
foreach (var num in list1)
{
    Console.WriteLine(num);
}
var sum2 = orders.Sum(x => x.Items.Sum(p => p.Quantity * p.UnitPrice));
Console.WriteLine("sum 2");
Console.WriteLine(sum2);
var list3 = orders.SelectMany(x => x.Items)
.Join(products, item => item.ProductId, product => product.Id
    , (item, product) => new
    {
        Category = product.Category,
        TotalPrice = item.Quantity * item.UnitPrice
    }).GroupBy(p => p.Category)
    .Select(group => new
    {
        Category = group.Key,
        Revenue = group.Sum(x => x.TotalPrice)
    }).ToList();
Console.WriteLine("list 3");
foreach (var num in list3)
{
    Console.WriteLine(num);
}
var list4F = orders.SelectMany(x => x.Items)
    .GroupBy(item => item.ProductId)
    .Select(group => new
    {
        ProductId = group.Key,
        TotalQuantity = group.Sum(x => x.Quantity)
    })
    .Join(products,
    order => order.ProductId,
    product => product.Id,
    (order, product) => new
    {
        Id = product.Id,
        Name = product.Name,
        quantity = order.TotalQuantity
    })
    .OrderByDescending(x => x.quantity).ThenBy(x => x.Id)
    .Take(3)
    .ToList();
Console.WriteLine("list 4f");
foreach (var num in list4F)
{
    Console.WriteLine(num);
}
var list5F = customers.GroupJoin(orders,
        customer => customer.Id,
        order => order.CustomerId,
        (customer, order) => new
        {
            Id = customer.Id,
            Name = customer.Name,
            NumberOfOrder = order.Count()
        }).Where(x => x.NumberOfOrder > 2)
        .ToList();
Console.WriteLine("list 5a");
foreach (var num in list5F)
{
    Console.WriteLine(num);
}
var list5B = orders.Where(x => x.Items == null || !x.Items.Any()).ToList();
Console.WriteLine("list 5b");
foreach (var num in list5B)
{
    Console.WriteLine(num);
}
var stopwatch = Stopwatch.StartNew();
decimal rate1 = await GetExchangeRateAsync(CancellationToken.None);
decimal rate2 = await GetExchangeRateAsync(CancellationToken.None);
decimal rate3 = await GetExchangeRateAsync(CancellationToken.None);
stopwatch.Stop();
Console.WriteLine($"Sequential: {stopwatch.ElapsedMilliseconds} ms");
stopwatch = Stopwatch.StartNew();
Task<decimal> rate4 = GetExchangeRateAsync(CancellationToken.None);
Task<decimal> rate5 = GetExchangeRateAsync(CancellationToken.None);
Task<decimal> rate6 = GetExchangeRateAsync(CancellationToken.None);
decimal[] rates = await Task.WhenAll(rate4, rate5, rate6);
stopwatch.Stop();
Console.WriteLine($"Sequential: {stopwatch.ElapsedMilliseconds} ms");

using var cts = new CancellationTokenSource(
    TimeSpan.FromMilliseconds(300)
);

try
{
    decimal rate = await GetExchangeRateAsync(cts.Token);
    Console.WriteLine(rate);
}
catch (OperationCanceledException)
{
    Console.WriteLine("End");
}
static async Task<decimal> GetExchangeRateAsync(CancellationToken ct)
{
    await Task.Delay(500, ct);
    return 25_00m;
}

List<int> numbers = new() { 1, 2, 3 };
var query = numbers.Where(x => x > 2);
numbers.Add(4);
foreach (int number in query)
{
    Console.WriteLine(number);
}
var snapshot = numbers.Where(x => x > 2).ToList(); // chạy ngay, lưu kết quả
numbers.Add(5);
Console.WriteLine(string.Join(",", snapshot));
// snapshot không có 5; query (không ToList) thì có
static async Task FailAsync() { await Task.Delay(100); throw new InvalidOperationException("boom"); }
static async Task SlowAsync() { await Task.Delay(500); Console.WriteLine("Slow finished"); }

var t1 = FailAsync();
var t2 = SlowAsync();
try { await Task.WhenAll(t1, t2); }

catch (Exception ex) { Console.WriteLine($"Caught: {ex.Message}"); }
// static async void BadAsync() { await Task.Delay(100); throw new Exception("boom"); }

// try { BadAsync(); }
// catch { Console.WriteLine("caught"); }   // dòng này KHÔNG chạy

// await Task.Delay(500);                    // chương trình sẽ crash trước khi kết thúc
public record Product(int Id, string Name, string Category, decimal Price, int Stock);
public record Customer(int Id, string Name);
public record OrderItem(int ProductId, int Quantity, decimal UnitPrice);
public record Order(int Id, int CustomerId, DateTime CreatedAt, string? Note, List<OrderItem> Items);