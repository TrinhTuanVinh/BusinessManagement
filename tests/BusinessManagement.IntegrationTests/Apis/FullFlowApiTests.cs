using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessManagement.Domain.Entities;
using BusinessManagement.Infrastructure.Persistence;
using BusinessManagement.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

[Collection("postgres")]
public sealed class FullFlowApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "CorrectHorse1!";
    private ApiFactory _root = null!;
    private WebApplicationFactory<Program> _app = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _connectionString = await postgres.CreateEmptyDatabaseAsync();
        _root = new ApiFactory();
        _app = _root.WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Default", _connectionString);
            b.UseSetting("Database:MigrateOnStartup", "true");
        });
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        await _root.DisposeAsync();
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {(int)expected} but got {(int)response.StatusCode}: {body}");
        if (body.Length == 0) return default;
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.Clone();
    }

    private static async Task<JsonElement> PostAsync(HttpClient client, string url, object body, HttpStatusCode expected)
        => await ReadAsync(await client.PostAsJsonAsync(url, body), expected);

    private static async Task<JsonElement> GetAsync(HttpClient client, string url, HttpStatusCode expected)
        => await ReadAsync(await client.GetAsync(url), expected);

    private HttpClient ClientWith(string? token)
    {
        var client = _app.CreateClient();
        if (token is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private async Task<string> LoginAsync(string email)
    {
        using var client = ClientWith(null);
        var login = await PostAsync(client, "/api/auth/login", new { email, password = Password }, HttpStatusCode.OK);
        return login.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> RegisterAndLoginAsync(string email)
    {
        using var client = ClientWith(null);
        await PostAsync(client, "/api/auth/register", new { email, password = Password }, HttpStatusCode.Created);
        return await LoginAsync(email);
    }

    [Fact]
    public async Task OrderLifecycle_RegisterLoginCreateCancel_WorksThroughHttp()
    {
        using var client = ClientWith(await RegisterAndLoginAsync($"{Guid.NewGuid():N}@test.com"));

        var customer = await PostAsync(client, "/api/customers",
            new { fullName = "Buyer", email = $"{Guid.NewGuid():N}@test.com" }, HttpStatusCode.Created);
        var product = await PostAsync(client, "/api/products",
            new { name = "Widget", category = "Tools", price = 100_000m, stock = 10 }, HttpStatusCode.Created);
        var customerId = customer.GetProperty("id").GetGuid();
        var productId = product.GetProperty("id").GetGuid();

        var order = await PostAsync(client, "/api/orders",
            new { customerId, items = new[] { new { productId, quantity = 2 } } }, HttpStatusCode.Created);
        var orderId = order.GetProperty("id").GetGuid();
        Assert.Equal("Pending", order.GetProperty("status").GetString());
        Assert.Equal(200_000m, order.GetProperty("total").GetDecimal());

        var afterOrder = await GetAsync(client, $"/api/products/{productId}", HttpStatusCode.OK);
        Assert.Equal(8, afterOrder.GetProperty("stock").GetInt32());

        var list = await GetAsync(client, $"/api/orders?customerId={customerId}", HttpStatusCode.OK);
        Assert.Equal(1, list.GetProperty("totalCount").GetInt32());

        var cancelled = await ReadAsync(await client.PostAsync($"/api/orders/{orderId}/cancel", null), HttpStatusCode.OK);
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());

        var afterCancel = await GetAsync(client, $"/api/products/{productId}", HttpStatusCode.OK);
        Assert.Equal(10, afterCancel.GetProperty("stock").GetInt32());           // hoàn kho đúng một lần

        await ReadAsync(await client.PostAsync($"/api/orders/{orderId}/cancel", null), HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AuthorizationAndErrors_AreMappedToExpectedStatusCodes()
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var staffToken = await RegisterAndLoginAsync(email);
        using var anonymous = ClientWith(null);
        using var staff = ClientWith(staffToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/reports/top-customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.DeleteAsync($"/api/products/{Guid.NewGuid()}")).StatusCode);

        var customerEmail = $"{Guid.NewGuid():N}@test.com";
        var customer = await PostAsync(staff, "/api/customers", new { fullName = "Buyer", email = customerEmail }, HttpStatusCode.Created);
        var product = await PostAsync(staff, "/api/products",
            new { name = "Rare", category = "Tools", price = 10_000m, stock = 1 }, HttpStatusCode.Created);
        var customerId = customer.GetProperty("id").GetGuid();
        var productId = product.GetProperty("id").GetGuid();

        await PostAsync(staff, "/api/orders", new { customerId, items = new[] { new { productId, quantity = 2 } } },
            HttpStatusCode.Conflict);                                                 // hết hàng: 409
        await PostAsync(staff, "/api/orders", new { customerId, items = new[] { new { productId, quantity = 1 } }, voucherCode = "NOPE" },
            HttpStatusCode.UnprocessableEntity);                                      // voucher sai: 422
        await PostAsync(staff, "/api/orders", new { customerId = Guid.Empty, items = new[] { new { productId, quantity = 1 } } },
            HttpStatusCode.BadRequest);                                               // cần [NotEmptyGuid]; chưa làm thì kỳ vọng 422
        await GetAsync(staff, $"/api/orders/{Guid.NewGuid()}", HttpStatusCode.NotFound);
        await PostAsync(staff, "/api/customers", new { fullName = "Dup", email = customerEmail }, HttpStatusCode.Conflict);

        // nâng quyền bằng SQL rồi đăng nhập lại (token cũ vẫn là Staff)
        await using (var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options))
            await db.Users.Where(u => u.Email == email).ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Admin));

        using var admin = ClientWith(await LoginAsync(email));
        await GetAsync(admin, "/api/reports/top-customers?top=5", HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/reports/top-customers?top=0")).StatusCode);
    }
}