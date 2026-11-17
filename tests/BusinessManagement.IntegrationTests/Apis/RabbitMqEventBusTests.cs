
using System.Text.Json;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

public sealed class RabbitMqEventBusTests : IClassFixture<RabbitFixture>
{
    private readonly RabbitFixture _fixture;

    public RabbitMqEventBusTests(RabbitFixture fixture)
    {
        _fixture = fixture;
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
            channel, CancellationToken.None);

        // Xóa message cũ trước mỗi test
        await channel.QueuePurgeAsync(
            RabbitMqTopology.OrderCreatedQueue);

        await channel.QueuePurgeAsync(
            RabbitMqTopology.OrderCreatedDlq);
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
    }
}
