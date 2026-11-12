using System.Net;
using System.Net.Http.Json;
using BusinessManagement.IntegrationTests.Helpers;

public class AuthApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient ClientWith(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/customers")]
    [InlineData("/api/reports/top-customers")]
    [InlineData("/api/auth/me")]
    public async Task ProtectedEndpoint_WithoutToken_Returns401(string url)
    {
        var client = factory.CreateClientFor(null);
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AllowHeath_WithoutToken_Returns200()
    {
        var client = factory.CreateClientFor(null);
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_StaffRole_Returns403()
    {
        var client = factory.CreateClientFor("Staff");
        var response = await client.GetAsync("/api/reports/top-customers");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidateParameter_Top0_Returns400()
    {
        var client = factory.CreateClientFor("Admin");
        var response = await client.GetAsync("/api/reports/top-customers?top=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_GetProduct_Returns401()
    {
        var client = ClientWith(TestTokens.Create(minutes: -10));
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongKey_GetProduct_Returns401()
    {
        var client = ClientWith(TestTokens.Create(key: new string('x', 40)));
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongAudience_GetProduct_Returns401()
    {
        var client = ClientWith(TestTokens.Create(audience: "other"));
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PasswordWith5Characters_Register_Returns400()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "a@b.com", password = "12345" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}