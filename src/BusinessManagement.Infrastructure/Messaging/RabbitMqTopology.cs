using BusinessManagement.Application.Messaging;
using RabbitMQ.Client;

namespace BusinessManagement.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string Exchange = "bm.events";
    public const string DeadLetterExchange = "bm.events.dlx";
    public const string OrderCreatedQueue = "bm.orders.created";
    public const string OrderCreatedDlq = "bm.orders.created.dlq";
    public const string RetryExchange = "bm.events.retry";
    public const string OrderCreatedRetryQueue = "bm.orders.created.retry";
    public static async Task DeclareAsync(IChannel channel, int retryDelayMs, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(OrderCreatedDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(OrderCreatedDlq, DeadLetterExchange, RoutingKeys.OrderCreated, cancellationToken: ct);

        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(OrderCreatedQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = RoutingKeys.OrderCreated
            },
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(OrderCreatedRetryQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = retryDelayMs,
                ["x-dead-letter-exchange"] = Exchange,
                ["x-dead-letter-routing-key"] = RoutingKeys.OrderCreated
            }, cancellationToken: ct);
        await channel.QueueBindAsync(OrderCreatedQueue, Exchange, RoutingKeys.OrderCreated, cancellationToken: ct);
        await channel.QueueBindAsync(OrderCreatedRetryQueue, RetryExchange, RoutingKeys.OrderCreated, cancellationToken: ct);
    }
}