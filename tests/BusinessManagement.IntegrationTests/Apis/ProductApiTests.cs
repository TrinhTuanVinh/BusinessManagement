
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessManagement.Api.Contracts.Requests;
using BusinessManagement.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

public class ProductApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ProductApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientFor("Staff");
    }
    private static bool HasError(JsonElement json, string field)
    => json.GetProperty("errors").EnumerateObject()
           .Any(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));


    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Product_MissingName_Returns400WithNameError()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("", "Product", 3m, 5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(HasError(await ReadJsonAsync(response), "Name"));
    }

    [Fact]
    public async Task Product_NegativePrice_Returns400WithPriceError()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Product 1", "Product", -1m, 5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(HasError(await ReadJsonAsync(response), "Price"));
    }

    [Fact]
    public async Task Product_PageSize1000_Returns400WithPageSizeError()
    {
        var response = await _client.GetAsync("/api/products?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(HasError(await ReadJsonAsync(response), "PageSize"));
    }

    [Fact]
    public async Task Product_IdIsNotAGuid_Returns404()
    {
        var response = await _client.GetAsync("/api/products/5");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    [Fact]
    public async Task Product_SortPassword_Returns400WithSortError()
    {
        var response = await _client.GetAsync("/api/products?sort=password");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(HasError(await ReadJsonAsync(response), "Sort"));
    }
    [Fact]
    public async Task Customer_PageSize1000_Returns400()          // sẽ ra 500 nếu [StringLength] còn gắn trên bool
    {
        var response = await _client.GetAsync("/api/customers?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(HasError(await ReadJsonAsync(response), "PageSize"));
    }

    [Fact]
    public async Task Customer_InvalidEmailWithoutPhone_ReportsOnlyEmail()   // Phone, Address phải là tuỳ chọn
    {
        var response = await _client.PostAsJsonAsync("/api/customers",
            new { fullName = "A", email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.True(HasError(json, "Email"));
        Assert.False(HasError(json, "Phone"));
        Assert.False(HasError(json, "Address"));
    }
}