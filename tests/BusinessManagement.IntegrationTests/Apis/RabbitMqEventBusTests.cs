
using System.Text;
using System.Text.Json;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Domain.Entities;
using BusinessManagement.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using BusinessManagement.Worker;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BusinessManagement.Application.Notifications;
using Xunit.Abstractions;

[Collection("postgres")]
public sealed class RabbitMqEventBusTests : IClassFixture<RabbitFixture>
{
    private readonly RabbitFixture _fixture;
    private readonly PostgresFixture _postgre;
    private readonly ITestOutputHelper _output;

    public RabbitMqEventBusTests(RabbitFixture fixture, PostgresFixture postgre, ITestOutputHelper output)
    {
        _fixture = fixture;
        _postgre = postgre;
        _output = output;
    }

    private async Task PrepareQueuesAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _fixture.Options.Host,
            Port = _fixture.Options.Port,
            UserName = _fixture.Options.UserName,
            Password = _fixture.Options.Password,
            VirtualHost = _fixture.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Đảm bảo topology đã tồn tại
        await RabbitMqTopology.DeclareAsync(
            channel, _fixture.Options.RetryDelayMs, CancellationToken.None);

        // Xóa message cũ trước mỗi test
        await channel.QueuePurgeAsync(
            RabbitMqTopology.OrderCreatedQueue);

        await channel.QueuePurgeAsync(
            RabbitMqTopology.OrderCreatedDlq);

        await channel.QueuePurgeAsync(
      RabbitMqTopology.OrderCreatedRetryQueue);
    }

    private IHost CreateWorkerHost(Action<IServiceCollection>? overrides = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = _postgre.ConnectionString,
            ["RabbitMq:Host"] = _fixture.Options.Host,
            ["RabbitMq:Port"] = _fixture.Options.Port.ToString(),
            ["RabbitMq:UserName"] = _fixture.Options.UserName,
            ["RabbitMq:Password"] = _fixture.Options.Password,
            ["RabbitMq:VirtualHost"] = _fixture.Options.VirtualHost
        });
        builder.Services.AddWorkerServices(builder.Configuration);
        overrides?.Invoke(builder.Services);
        return builder.Build();
    }
    private static OrderCreated CreateEvent()
    {
        return new OrderCreated(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            150_000m,
            DateTime.UtcNow);
    }

    private async Task<IChannel> CreateChannelAsync(
        IConnection connection)
    {
        return await connection.CreateChannelAsync();
    }

    [Fact]
    public async Task PublishAsync_PublishesPersistentJsonMessage()
    {
        await PrepareQueuesAsync();

        await using var bus = new RabbitMqEventBus(_fixture.Options);

        var message = CreateEvent();

        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            message,
            CancellationToken.None);

        var factory = new ConnectionFactory
        {
            HostName = _fixture.Options.Host,
            Port = _fixture.Options.Port,
            UserName = _fixture.Options.UserName,
            Password = _fixture.Options.Password,
            VirtualHost = _fixture.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await CreateChannelAsync(connection);

        var result = await channel.BasicGetAsync(
            RabbitMqTopology.OrderCreatedQueue,
            autoAck: true);

        Assert.NotNull(result);

        var actual = JsonSerializer.Deserialize<OrderCreated>(
            result.Body.Span);

        Assert.Equal(message, actual);
        Assert.Equal(
            "application/json",
            result.BasicProperties.ContentType);

        Assert.Equal(
            DeliveryModes.Persistent,
            result.BasicProperties.DeliveryMode);
    }

    [Fact]
    public async Task PublishAsync_Twice_PublishesTwoMessages()
    {
        await PrepareQueuesAsync();

        await using var bus = new RabbitMqEventBus(_fixture.Options);

        var first = CreateEvent();
        var second = CreateEvent();

        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            first,
            CancellationToken.None);

        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            second,
            CancellationToken.None);

        var factory = new ConnectionFactory
        {
            HostName = _fixture.Options.Host,
            Port = _fixture.Options.Port,
            UserName = _fixture.Options.UserName,
            Password = _fixture.Options.Password,
            VirtualHost = _fixture.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await CreateChannelAsync(connection);

        var firstResult = await channel.BasicGetAsync(
            RabbitMqTopology.OrderCreatedQueue,
            autoAck: true);

        var secondResult = await channel.BasicGetAsync(
            RabbitMqTopology.OrderCreatedQueue,
            autoAck: true);

        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);

        var firstActual = JsonSerializer.Deserialize<OrderCreated>(
            firstResult.Body.Span);

        var secondActual = JsonSerializer.Deserialize<OrderCreated>(
            secondResult.Body.Span);

        Assert.Equal(first, firstActual);
        Assert.Equal(second, secondActual);
    }

    [Fact]
    public async Task PublishAsync_RejectWithoutRequeue_MovesMessageToDlq()
    {
        await PrepareQueuesAsync();

        await using var bus = new RabbitMqEventBus(_fixture.Options);

        var message = CreateEvent();

        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            message,
            CancellationToken.None);

        var factory = new ConnectionFactory
        {
            HostName = _fixture.Options.Host,
            Port = _fixture.Options.Port,
            UserName = _fixture.Options.UserName,
            Password = _fixture.Options.Password,
            VirtualHost = _fixture.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await CreateChannelAsync(connection);

        var result = await channel.BasicGetAsync(
            RabbitMqTopology.OrderCreatedQueue,
            autoAck: false);

        Assert.NotNull(result);

        var original = JsonSerializer.Deserialize<OrderCreated>(
            result.Body.Span);

        Assert.Equal(message, original);

        // 2. Reject message, không requeue
        await channel.BasicRejectAsync(
            result.DeliveryTag,
            requeue: false);

        // 3. Kiểm tra queue chính không còn message
        var mainQueueResult = await channel.BasicGetAsync(
            RabbitMqTopology.OrderCreatedQueue,
            autoAck: true);

        Assert.Null(mainQueueResult);

        // 4. Kiểm tra message đã được chuyển sang DLQ
        var dlqResult = await channel.BasicGetAsync(
            RabbitMqTopology.OrderCreatedDlq,
            autoAck: true);

        Assert.NotNull(dlqResult);

        var deadLetter = JsonSerializer.Deserialize<OrderCreated>(
            dlqResult.Body.Span);

        Assert.Equal(message, deadLetter);
    }

    [Fact]
    public async Task PublishAsync_UnroutableRoutingKey_ObserveBehavior()
    {
        await PrepareQueuesAsync();

        await using var bus = new RabbitMqEventBus(_fixture.Options);

        var message = CreateEvent();

        var exception = await Record.ExceptionAsync(
            () => bus.PublishAsync(
                "nobody.listens",
                message,
                CancellationToken.None));
        _output.WriteLine(exception is null ? "NO EXCEPTION" : $"{exception.GetType().FullName}: {exception.Message}");
        Assert.NotNull(exception);     // tin sai routing key không được biến mất im lặng
    }
    [Fact]
    public async Task ConsumeAsync_ValidMessage_CreatesNotification()
    {
        await PrepareQueuesAsync();

        using var host = CreateWorkerHost();

        await host.StartAsync();

        var message = CreateEvent();

        await using var bus = new RabbitMqEventBus(_fixture.Options);

        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            message,
            CancellationToken.None);

        var timeout = DateTime.UtcNow.AddSeconds(10);
        Notification? notification = null;

        while (DateTime.UtcNow < timeout)
        {
            await using var db = _postgre.CreateContext();

            notification = await db.Notifications
                .FirstOrDefaultAsync(x => x.OrderId == message.OrderId);

            if (notification is not null)
                break;

            await Task.Delay(100);
        }

        Assert.NotNull(notification);

        await host.StopAsync();
    }
    [Fact]
    public async Task ConsumeAsync_InvalidJson_MovesMessageToDlq()
    {
        await PrepareQueuesAsync();

        using var provider = CreateWorkerHost();
        await provider.StartAsync(CancellationToken.None);

        var factory = new ConnectionFactory
        {
            HostName = _fixture.Options.Host,
            Port = _fixture.Options.Port,
            UserName = _fixture.Options.UserName,
            Password = _fixture.Options.Password,
            VirtualHost = _fixture.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.BasicPublishAsync(
            exchange: RabbitMqTopology.Exchange,
            routingKey: RoutingKeys.OrderCreated,
            mandatory: false,
            basicProperties: new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            },
            body: Encoding.UTF8.GetBytes("not json"));

        BasicGetResult? dlqResult = null;
        var timeout = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < timeout)
        {
            dlqResult = await channel.BasicGetAsync(
                RabbitMqTopology.OrderCreatedDlq,
                autoAck: true);

            if (dlqResult is not null)
                break;

            await Task.Delay(100);
        }

        Assert.NotNull(dlqResult);

        var mainResult = await channel.BasicGetAsync(
            RabbitMqTopology.OrderCreatedQueue,
            autoAck: true);

        Assert.Null(mainResult);

        await provider.StopAsync(CancellationToken.None);
    }
    private sealed class ThrowingHandler : IOrderCreatedHandler
    {
        public Task HandleAsync(OrderCreated message, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task ConsumeAsync_HandlerThrows_MovesMessageToDlq()
    {
        await PrepareQueuesAsync();

        using var host = CreateWorkerHost(s => s.AddScoped<IOrderCreatedHandler, ThrowingHandler>());

        await host.StartAsync(CancellationToken.None);

        var message = CreateEvent();

        await using var bus = new RabbitMqEventBus(_fixture.Options);
        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            message,
            CancellationToken.None);

        var factory = new ConnectionFactory
        {
            HostName = _fixture.Options.Host,
            Port = _fixture.Options.Port,
            UserName = _fixture.Options.UserName,
            Password = _fixture.Options.Password,
            VirtualHost = _fixture.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        BasicGetResult? dlqResult = null;
        var timeout = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < timeout)
        {
            dlqResult = await channel.BasicGetAsync(
                RabbitMqTopology.OrderCreatedDlq,
                autoAck: true);

            if (dlqResult is not null)
                break;

            await Task.Delay(100);
        }

        Assert.NotNull(dlqResult);
        Assert.NotNull(dlqResult.BasicProperties.Headers);
        Assert.Contains("x-death", dlqResult.BasicProperties.Headers.Keys);

        await host.StopAsync(CancellationToken.None);
    }
    [Fact]
    public async Task ConsumeAsync_ConsumerStopped_MessageRemainsAndIsProcessedAfterRestart()
    {
        await PrepareQueuesAsync();

        using var provider = CreateWorkerHost();
        await provider.StartAsync(CancellationToken.None);
        await provider.StopAsync(CancellationToken.None);

        var message = CreateEvent();

        await using var bus = new RabbitMqEventBus(_fixture.Options);
        await bus.PublishAsync(
            RoutingKeys.OrderCreated,
            message,
            CancellationToken.None);

        var factory = new ConnectionFactory
        {
            HostName = _fixture.Options.Host,
            Port = _fixture.Options.Port,
            UserName = _fixture.Options.UserName,
            Password = _fixture.Options.Password,
            VirtualHost = _fixture.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Kiểm tra message đang Ready mà không lấy message khỏi queue.
        var queueInfo = await channel.QueueDeclarePassiveAsync(
            RabbitMqTopology.OrderCreatedQueue);

        Assert.Equal(1u, queueInfo.MessageCount);
        using var host = CreateWorkerHost();
        await host.StartAsync(CancellationToken.None);

        var timeout = DateTime.UtcNow.AddSeconds(10);
        Notification? notification = null;

        while (DateTime.UtcNow < timeout)
        {
            await using var db = _postgre.CreateContext();

            notification = await db.Notifications
                .FirstOrDefaultAsync(x => x.OrderId == message.OrderId);

            if (notification is not null)
                break;

            await Task.Delay(100);
        }

        Assert.NotNull(notification);

        await host.StopAsync(CancellationToken.None);
    }
}
