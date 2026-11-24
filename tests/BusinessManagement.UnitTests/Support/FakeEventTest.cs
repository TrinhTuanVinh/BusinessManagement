using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using BusinessManagement.Application.Messaging;

public sealed class FakeEventBus : IEventBus
{
    public sealed record PublishedMessage(string RoutingKey, string Type, Guid MessageId, string Body);

    private readonly ConcurrentQueue<PublishedMessage> _published = new();
    private int _attempts;

    public IReadOnlyCollection<PublishedMessage> Published => _published.ToArray();
    public Exception? ThrowOnPublish { get; set; }
    public int? ThrowFromAttempt { get; set; }
    public TimeSpan PublishDelay { get; set; }

    public Task PublishAsync<T>(string routingKey, T message, CancellationToken ct)
        => PublishRawAsync(routingKey, typeof(T).Name, Guid.NewGuid(), JsonSerializer.SerializeToUtf8Bytes(message), ct);

    public async Task PublishRawAsync(string routingKey, string type, Guid messageId,
        ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        if (PublishDelay > TimeSpan.Zero) await Task.Delay(PublishDelay, ct);

        if (ThrowOnPublish is not null) throw ThrowOnPublish;
        if (ThrowFromAttempt is { } from && attempt >= from) throw new InvalidOperationException("broker down");

        _published.Enqueue(new PublishedMessage(routingKey, type, messageId, Encoding.UTF8.GetString(body.Span)));
    }
}