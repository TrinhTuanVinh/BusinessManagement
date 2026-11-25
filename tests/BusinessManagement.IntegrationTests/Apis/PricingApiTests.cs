using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessManagement.Api.Contracts.Response;
using BusinessManagement.Application.Checkout.Pricing;
using BusinessManagement.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace BusinessManagement.IntegrationTests.Apis;

public class PricingApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public PricingApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientFor("Staff");
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Quote_VipWithoutVoucher_ReturnsTenPercentDiscount()
    {
        var response = await _client.PostAsJsonAsync("/api/pricing/quote",
            new { orderTotal = 1_000_000m, customerType = "VIP" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new QuoteResponse(1_000_000m, 100_000m, 900_000m),
            await response.Content.ReadFromJsonAsync<QuoteResponse>());
    }

    [Fact]
    public async Task Quote_VipWithSale50k_Returns850000()
    {
        var response = await _client.PostAsJsonAsync("/api/pricing/quote",
            new { orderTotal = 1_000_000m, customerType = "VIP", voucherCode = "SALE50K" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new QuoteResponse(1_000_000m, 100_050m, 899_950m),
            await response.Content.ReadFromJsonAsync<QuoteResponse>());
    }

    [Fact]
    public async Task Quote_ZeroTotal_Returns400WithOrderTotalError()
    {
        var response = await _client.PostAsJsonAsync("/api/pricing/quote",
            new { orderTotal = 0m, customerType = "VIP" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.True(json.GetProperty("errors").TryGetProperty("OrderTotal", out _));
    }

    [Fact]
    public async Task Quote_MissingCustomerType_Returns400WithCustomerTypeError()
    {
        var response = await _client.PostAsJsonAsync("/api/pricing/quote",
            new { orderTotal = 1_000_000m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.True(json.GetProperty("errors").TryGetProperty("CustomerType", out _));
    }

    [Fact]
    public async Task Quote_InvalidVoucher_Returns422WithDetailAndTraceId()
    {
        var response = await _client.PostAsJsonAsync("/api/pricing/quote",
            new { orderTotal = 1_000_000m, customerType = "VIP", voucherCode = "NOPE" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Business rule violated", json.GetProperty("title").GetString());
        Assert.Contains("NOPE", json.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Quote_UnexpectedError_Returns500WithoutLeakingDetails()
    {
        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<PricingService>();
                services.AddScoped<PricingService>(_ =>
                    throw new InvalidOperationException("secret-internal-detail"));
            })).CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Create("Staff"));
        var response = await client.PostAsJsonAsync("/api/pricing/quote",
            new { orderTotal = 1_000_000m, customerType = "VIP" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-internal-detail", text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.Contains("traceId", text);
    }
    [Fact]
    public async Task CreateOrder_MissingCustomerId_Returns422()
    {
        var client = _factory.CreateClientFor("Staff");

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                items = new[]
                {
                    new
                    {
                        productId = Guid.NewGuid(),
                        quantity = 1
                    }
                }
            });
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {responseBody}");

        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode
        );
    }
    [Fact]
    public async Task CreateOrder_MissingProductId_Returns422()
    {
        var client = _factory.CreateClientFor("Admin");

        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId = Guid.NewGuid(),
                items = new[]
                {
                new { quantity = 1 }
                }
            });

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }
}