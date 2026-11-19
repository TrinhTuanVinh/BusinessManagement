using System.Text.Json;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Application.Notifications;
using BusinessManagement.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BusinessManagement.Worker;

public sealed class OrderCreatedConsumer(
    RabbitMqOptions options, IServiceScopeFactory scopes, ILogger<OrderCreatedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Consumer stopped unexpectedly; reconnecting in 5 seconds");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch (OperationCanceledException) { }
            }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            VirtualHost = options.VirtualHost,
            UserName = options.UserName,
            Password = options.Password,
            AutomaticRecoveryEnabled = true
        };
        await using var connection = await factory.CreateConnectionAsync(ct);       // lần đầu lỗi thì vòng lặp ở trên thử lại
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await RabbitMqTopology.DeclareAsync(channel, ct);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => HandleAsync(channel, ea, ct);
        await channel.BasicConsumeAsync(RabbitMqTopology.OrderCreatedQueue, autoAck: false, consumer, ct);

        logger.LogInformation("Consuming from {Queue}", RabbitMqTopology.OrderCreatedQueue);
        await Task.Delay(Timeout.Infinite, ct);                                     // chạy đến khi bị dừng
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        OrderCreated? message = null;
        try { message = JsonSerializer.Deserialize<OrderCreated>(ea.Body.Span); }
        catch (JsonException ex) { logger.LogError(ex, "Malformed message {MessageId}", ea.BasicProperties.MessageId); }

        if (message is null || message.EventId == Guid.Empty)
        {
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);   // tin độc: sang DLQ, không lặp
            return;
        }

        using var scope = logger.BeginScope(new Dictionary<string, object>
        { ["EventId"] = message.EventId, ["OrderId"] = message.OrderId });
        try
        {
            await using var serviceScope = scopes.CreateAsyncScope();                // một scope cho mỗi tin
            await serviceScope.ServiceProvider.GetRequiredService<IOrderCreatedHandler>().HandleAsync(message, ct);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);        // ack SAU khi xử lý xong
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // đang tắt: không ack, không nack; broker sẽ giao lại cho lần chạy sau
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Handler failed (redelivered: {Redelivered})", ea.Redelivered);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);   // Ngày 16: thử lại có độ trễ trước khi vào DLQ
        }
    }
}