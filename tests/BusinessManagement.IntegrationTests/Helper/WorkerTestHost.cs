using BusinessManagement.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class WorkerTestHost
{
    public static IHost Create(PostgresFixture pg, RabbitFixture rabbit,
        bool outboxEnabled = false, Action<IServiceCollection>? overrides = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = pg.ConnectionString,
            ["RabbitMq:Host"] = rabbit.Options.Host,
            ["RabbitMq:Port"] = rabbit.Options.Port.ToString(),
            ["RabbitMq:UserName"] = rabbit.Options.UserName,
            ["RabbitMq:Password"] = rabbit.Options.Password,
            ["RabbitMq:VirtualHost"] = rabbit.Options.VirtualHost,
            ["RabbitMq:RetryDelayMs"] = "300",
            ["RabbitMq:MaxAttempts"] = "3",
            ["Outbox:Enabled"] = outboxEnabled ? "true" : "false",     // test consumer cũ phải tắt relay
            ["Outbox:PollIntervalMs"] = "100"
        });
        builder.Services.AddWorkerServices(builder.Configuration);
        overrides?.Invoke(builder.Services);
        return builder.Build();
    }
}