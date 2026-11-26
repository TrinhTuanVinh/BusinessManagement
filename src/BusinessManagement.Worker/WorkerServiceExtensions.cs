using BusinessManagement.Application.Notifications;
using BusinessManagement.Infrastructure;
using BusinessManagement.Infrastructure.Messaging;

namespace BusinessManagement.Worker;

public static class WorkerServiceExtensions
{
    public static IServiceCollection AddWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddMessaging(configuration);

        services.AddScoped<IOrderCreatedHandler, OrderCreatedHandler>();
        services.AddHostedService<OrderCreatedConsumer>();
        services.AddScoped<OutboxRelayService>();
        if (configuration.GetValue("Outbox:Enabled", true))
            services.AddHostedService<OutboxRelayWorker>();
        return services;
    }
}