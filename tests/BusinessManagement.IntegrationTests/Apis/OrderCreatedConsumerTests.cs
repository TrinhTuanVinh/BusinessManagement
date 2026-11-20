
using BusinessManagement.Application.Messaging;
using BusinessManagement.Application.Notifications;
using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;
using BusinessManagement.Infrastructure.Messaging;
using BusinessManagement.Infrastructure.Persistence;
using BusinessManagement.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Xunit;

[Collection("postgres")]
public sealed class OrderCreatedConsumerTests : IClassFixture<RabbitFixture>
{
    private readonly RabbitFixture _rabbit;
    private readonly PostgresFixture _postgres;

    public OrderCreatedConsumerTests(
         PostgresFixture postgres,
        RabbitFixture rabbit)

    {
        _rabbit = rabbit;
        _postgres = postgres;
    }

    private OrderCreated CreateEvent(Guid? eventId = null)
    {
        return new OrderCreated(
            eventId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            150_000m,
            DateTime.UtcNow);
    }

    private IHost CreateWorkerHost(Action<IServiceCollection>? overrides = null)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.ConnectionString,
                ["RabbitMq:Host"] = _rabbit.Options.Host,
                ["RabbitMq:Port"] = _rabbit.Options.Port.ToString(),
                ["RabbitMq:UserName"] = _rabbit.Options.UserName,
                ["RabbitMq:Password"] = _rabbit.Options.Password,
                ["RabbitMq:VirtualHost"] = _rabbit.Options.VirtualHost,
                ["RabbitMq:RetryDelayMs"] = "300",
                ["RabbitMq:MaxAttempts"] = "3"
            });

        builder.Services.AddWorkerServices(builder.Configuration);
        overrides?.Invoke(builder.Services);

        return builder.Build();
    }

    private async Task PrepareQueuesAsync()
    {
        var factory = CreateFactory();

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await RabbitMqTopology.DeclareAsync(
            channel,
            _rabbit.Options.RetryDelayMs,
            CancellationToken.None);

        await channel.QueuePurgeAsync(RabbitMqTopology.OrderCreatedQueue);
        await channel.QueuePurgeAsync(RabbitMqTopology.OrderCreatedDlq);
        await channel.QueuePurgeAsync(RabbitMqTopology.OrderCreatedRetryQueue);
    }

    private ConnectionFactory CreateFactory()
    {
        return new ConnectionFactory
        {
            HostName = _rabbit.Options.Host,
            Port = _rabbit.Options.Port,
            UserName = _rabbit.Options.UserName,
            Password = _rabbit.Options.Password,
            VirtualHost = _rabbit.Options.VirtualHost
        };
    }

    private async Task PublishAsync(OrderCreated message)
    {
        await using var bus = new RabbitMqEventBus(_rabbit.Options);

        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            message,
            CancellationToken.None);
    }

    private async Task<uint> GetQueueCountAsync(string queue)
    {
        await using var connection =
            await CreateFactory().CreateConnectionAsync();

        await using var channel =
            await connection.CreateChannelAsync();

        var result = await channel.QueueDeclarePassiveAsync(queue);

        return result.MessageCount;
    }

    private async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(100);
        }

        Assert.Fail("Condition was not satisfied before timeout.");
    }

    private async Task<(int Notifications, int Processed)> GetDatabaseCountsAsync(
        OrderCreated message)
    {
        await using var db = _postgres.CreateContext();

        var notifications = await db.Notifications
            .CountAsync(x => x.OrderId == message.OrderId);

        var processed = await db.ProcessedMessages
            .CountAsync(x =>
                x.EventId == message.EventId &&
                x.Consumer == OrderCreatedHandler.ConsumerName);

        return (notifications, processed);
    }

    // Test 4:
    // Publish cùng một EventId hai lần.
    [Fact]
    public async Task ConsumeAsync_DuplicateMessage_CreatesOneNotification()
    {
        await PrepareQueuesAsync();

        using var host = CreateWorkerHost();
        await host.StartAsync();

        var message = CreateEvent();

        await PublishAsync(message);
        await PublishAsync(message);

        await WaitUntilAsync(async () =>
        {
            var counts = await GetDatabaseCountsAsync(message);
            var queueCount = await GetQueueCountAsync(
                RabbitMqTopology.OrderCreatedQueue);

            return counts.Notifications == 1 &&
                   counts.Processed == 1 &&
                   queueCount == 0;
        });

        var finalCounts = await GetDatabaseCountsAsync(message);

        Assert.Equal(1, finalCounts.Notifications);
        Assert.Equal(1, finalCounts.Processed);

        Assert.Equal(
            0u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedQueue));

        Assert.Equal(
            0u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedDlq));

        await host.StopAsync();
    }

    // Test 5:
    // Hai lần đầu throw, lần thứ ba thành công.
    [Fact]
    public async Task ConsumeAsync_FlakyHandler_RetriesAndSucceeds()
    {
        await PrepareQueuesAsync();

        var flaky = new FlakyHandler(
            failFirst: 2,
            _postgres);

        using var host = CreateWorkerHost(services =>
        {
            services.AddSingleton(flaky);

            services.AddScoped<IOrderCreatedHandler>(
                sp => sp.GetRequiredService<FlakyHandler>());
        });

        await host.StartAsync();

        var message = CreateEvent();

        await PublishAsync(message);

        await WaitUntilAsync(async () =>
        {
            var counts = await GetDatabaseCountsAsync(message);

            return flaky.Calls == 3 &&
                   counts.Notifications == 1 &&
                   counts.Processed == 1 &&
                   await GetQueueCountAsync(
                       RabbitMqTopology.OrderCreatedQueue) == 0;
        });

        Assert.Equal(3, flaky.Calls);

        var finalCounts = await GetDatabaseCountsAsync(message);

        Assert.Equal(1, finalCounts.Notifications);
        Assert.Equal(1, finalCounts.Processed);

        Assert.Equal(
            0u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedDlq));

        await host.StopAsync();
    }

    // Test 6:
    // Handler luôn throw, tối đa 3 lần gọi.
    [Fact]
    public async Task ConsumeAsync_AlwaysFailingHandler_MovesToDlqAfterMaxAttempts()
    {
        await PrepareQueuesAsync();

        var handler = new AlwaysFailingHandler();

        using var host = CreateWorkerHost(services =>
        {
            services.AddSingleton(handler);

            services.AddScoped<IOrderCreatedHandler>(
                sp => sp.GetRequiredService<AlwaysFailingHandler>());
        });

        await host.StartAsync();

        var message = CreateEvent();

        await PublishAsync(message);

        await WaitUntilAsync(async () =>
        {
            return handler.Calls == 3 &&
                   await GetQueueCountAsync(
                       RabbitMqTopology.OrderCreatedDlq) == 1;
        });

        Assert.Equal(3, handler.Calls);

        Assert.Equal(
            0u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedQueue));

        Assert.Equal(
            0u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedRetryQueue));

        Assert.Equal(
            1u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedDlq));

        var counts = await GetDatabaseCountsAsync(message);

        Assert.Equal(0, counts.Notifications);
        Assert.Equal(0, counts.Processed);

        await host.StopAsync();
    }

    // Test 7:
    // DomainException phải chuyển DLQ ngay.
    [Fact]
    public async Task ConsumeAsync_DomainException_MovesToDlqImmediately()
    {
        await PrepareQueuesAsync();

        var handler = new DomainFailingHandler();

        using var host = CreateWorkerHost(services =>
        {
            services.AddSingleton(handler);

            services.AddScoped<IOrderCreatedHandler>(
                sp => sp.GetRequiredService<DomainFailingHandler>());
        });

        await host.StartAsync();

        var message = CreateEvent();

        await PublishAsync(message);

        await WaitUntilAsync(async () =>
        {
            return handler.Calls == 1 &&
                   await GetQueueCountAsync(
                       RabbitMqTopology.OrderCreatedDlq) == 1;
        });

        Assert.Equal(1, handler.Calls);

        Assert.Equal(
            0u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedQueue));

        Assert.Equal(
            0u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedRetryQueue));

        Assert.Equal(
            1u,
            await GetQueueCountAsync(RabbitMqTopology.OrderCreatedDlq));

        var counts = await GetDatabaseCountsAsync(message);

        Assert.Equal(0, counts.Notifications);
        Assert.Equal(0, counts.Processed);

        await host.StopAsync();
    }

    private sealed class FlakyHandler : IOrderCreatedHandler
    {
        private readonly int _failFirst;
        private readonly PostgresFixture _postgres;
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public FlakyHandler(int failFirst, PostgresFixture postgres)
        {
            _failFirst = failFirst;
            _postgres = postgres;
        }

        public async Task HandleAsync(
            OrderCreated message,
            CancellationToken ct)
        {
            if (Interlocked.Increment(ref _calls) <= _failFirst)
                throw new InvalidOperationException("boom");

            // Lần thành công cuối cùng gọi Handler thật.
            await using var db = _postgres.CreateContext();

            var handler = new OrderCreatedHandler(
                db,
                Microsoft.Extensions.Logging.Abstractions
                    .NullLogger<OrderCreatedHandler>.Instance);

            await handler.HandleAsync(message, ct);
        }
    }

    private sealed class AlwaysFailingHandler : IOrderCreatedHandler
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task HandleAsync(OrderCreated message, CancellationToken ct)
        {
            Interlocked.Increment(ref _calls);
            throw new InvalidOperationException("always fails");
        }
    }

    private sealed class DomainFailingHandler : IOrderCreatedHandler
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task HandleAsync(OrderCreated message, CancellationToken ct)
        {
            Interlocked.Increment(ref _calls);
            throw new DomainException("Invalid order event");
        }
    }
}
