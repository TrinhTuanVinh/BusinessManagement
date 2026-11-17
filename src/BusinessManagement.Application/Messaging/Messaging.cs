namespace BusinessManagement.Application.Messaging;

public record OrderCreated(Guid EventId, Guid OrderId, Guid CustomerId, decimal Total, DateTime OccurredAtUtc);

public static class RoutingKeys
{
    public const string OrderCreated = "order.created";
}

public interface IEventBus
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct);
}