using System.Net;
using System.Text.RegularExpressions;
using BusinessManagement.IntegrationTests.Helpers;

public class RequestIdApiTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task WithoutRequestId_ReturnsGeneratedRequestId()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues(
            "X-Request-Id", out var values));

        var requestId = Assert.Single(values);

        Assert.Matches("^[a-fA-F0-9]{32}$", requestId);
    }

    [Fact]
    public async Task ValidRequestId_ReturnsSameRequestId()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Request-Id", "abc12345-test");

        var response = await client.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues(
            "X-Request-Id", out var values));

        Assert.Equal("abc12345-test", Assert.Single(values));
    }

    [Fact]
    public async Task InvalidRequestId_ReturnsNewRequestId()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Request-Id", "bad id with spaces");

        var response = await client.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues(
            "X-Request-Id", out var values));

        var requestId = Assert.Single(values);

        Assert.NotEqual("bad id with spaces", requestId);
        Assert.Matches("^[a-fA-F0-9]{32}$", requestId);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsRequestId()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.Headers.Contains("X-Request-Id"));
    }

    [Fact]
    public async Task UnauthorizedRequest_ReturnsRequestId()
    {
        var client = factory.CreateClientFor(null);

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Request-Id"));
    }
}