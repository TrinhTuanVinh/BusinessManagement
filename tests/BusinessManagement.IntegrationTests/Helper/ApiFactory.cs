using BusinessManagement.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BusinessManagement.IntegrationTests.Helpers;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Issuer", TestTokens.Issuer);
        builder.UseSetting("Jwt:Audience", TestTokens.Audience);
        builder.UseSetting("Jwt:Key", TestTokens.Key);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<JwtOptions>();
            services.AddSingleton(new JwtOptions
            { Issuer = TestTokens.Issuer, Audience = TestTokens.Audience, Key = TestTokens.Key });
        });
    }

    public HttpClient CreateClientFor(string? role)
    {
        var client = CreateClient();
        if (role is not null)
            client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Create(role));
        return client;
    }
}