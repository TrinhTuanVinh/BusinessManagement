using BusinessManagement.Infrastructure.Messaging;
using BusinessManagement.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class WorkerCompositionTests
{
    private static Dictionary<string, string?> ValidConfig() => new()
    {
        ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=u;Password=p",
        ["RabbitMq:Host"] = "localhost",
        ["RabbitMq:UserName"] = "u",
        ["RabbitMq:Password"] = "p"
    };

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);     
        services.AddWorkerServices(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public async Task Worker_BuildsWithScopeValidation_AndResolvesRelay()
    {
        await using var provider = (ServiceProvider)Build(ValidConfig());
        await using var scope = provider.CreateAsyncScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OutboxRelayService>());
    }

    [Fact]
    public void Worker_MissingRabbitPassword_FailsFast()
    {
        var settings = ValidConfig();
        settings.Remove("RabbitMq:Password");

        var ex = Assert.Throws<InvalidOperationException>(() => Build(settings));

        Assert.Contains("RabbitMq", ex.Message);
    }
}