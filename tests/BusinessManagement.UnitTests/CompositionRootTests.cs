using BusinessManagement.Application;
using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Checkout.Events;
using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.Infrastructure;
using BusinessManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class CompositionRootTests
{
    private static ServiceProvider Build(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["Payments:Bank:MerchantAccount"] = "TEST-ACCOUNT",
                ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=u;Password=p",
                ["Jwt:Issuer"] = "i",
                ["Jwt:Audience"] = "a",
                ["Jwt:Key"] = new string('k', 40),
                ["RabbitMq:Host"] = "c",
                ["RabbitMq:UserName"] = "d",
                ["RabbitMq:Password"] = "e"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    [Fact]
    public void Container_BuildsAndResolvesUseCases()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RefundProcessor>());
    }

    [Fact]
    public void PaymentMethods_HaveUniqueKeysAndIncludeBank()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        var keys = scope.ServiceProvider.GetServices<IPaymentMethod>()
            .Select(p => p.PaymentMethod).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(keys, k => k.Equals("Bank", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddInfrastructure_MissingMerchantAccount_FailsFast()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Build(new Dictionary<string, string?>()));

        Assert.Contains("MerchantAccount", ex.Message);
    }
    [Fact]
    public void DbContext_IsScoped_AndInterfaceResolvesToSameInstance()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        Assert.Same(scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IAppDbContext>());
    }

    [Fact]
    public void AddInfrastructure_MissingConnectionString_FailsFast()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Build(new Dictionary<string, string?> { ["Payments:Bank:MerchantAccount"] = "TEST" }));

        Assert.Contains("ConnectionStrings", ex.Message);
    }

    [Fact]
    public void AddInfrastructure_ShortJwtKey_FailsFast()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Build(new Dictionary<string, string?>
            {
                ["Payments:Bank:MerchantAccount"] = "TEST-ACCOUNT",
                ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=u;Password=p",
                ["Jwt:Issuer"] = "i",
                ["Jwt:Audience"] = "a",
                ["Jwt:Key"] = "abssc"
            }));

        Assert.Contains("Key", ex.Message);
    }

    [Fact]
    public void AddInfrastructure_MissingRabbitPassword_FailsFast()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
           Build(new Dictionary<string, string?>
           {
               ["Payments:Bank:MerchantAccount"] = "TEST-ACCOUNT",
               ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=u;Password=p",
               ["Jwt:Issuer"] = "i",
               ["Jwt:Audience"] = "a",
               ["Jwt:Key"] = "abssc",
               ["RabbitMq:Host"] = "c",
               ["RabbitMq:UserName"] = "d",
           }));

        Assert.Contains("Key", ex.Message);
    }
}