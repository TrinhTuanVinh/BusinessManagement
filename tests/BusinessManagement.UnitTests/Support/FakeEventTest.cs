using BusinessManagement.Application.Messaging;
namespace BusinessManagement.UnitTests.Support;

public sealed class FakeEventBus : IEventBus
{
    public List<(string RoutingKey, object Message)> Published { get; } = new();
    public Exception? ThrowOnPublish { get; set; }

    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct)
    {
        if (ThrowOnPublish is not null) throw ThrowOnPublish;
        Published.Add((routingKey, message!));
        return Task.CompletedTask;
    }
}