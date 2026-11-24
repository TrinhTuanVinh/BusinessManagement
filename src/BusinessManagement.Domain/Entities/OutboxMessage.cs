using BusinessManagement.Domain.Common;

namespace BusinessManagement.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    private OutboxMessage() { }

    public OutboxMessage(Guid id, string routingKey, string type, string payload)
    {
        if (id == Guid.Empty) throw new DomainException("Outbox message id is required.");
        if (string.IsNullOrWhiteSpace(routingKey)) throw new DomainException("Routing key is required.");
        if (string.IsNullOrWhiteSpace(type)) throw new DomainException("Type is required.");
        if (string.IsNullOrWhiteSpace(payload)) throw new DomainException("Payload is required.");
        Id = id; RoutingKey = routingKey.Trim(); Type = type.Trim(); Payload = payload;
        NextAttemptAt = CreatedAt;
    }

    public string RoutingKey { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTime NextAttemptAt { get; private set; }

    public void MarkProcessed(DateTime utcNow)
    {
        ProcessedAt = utcNow;
        LastError = null;
        Touch();
    }

    public void MarkFailed(string error, DateTime utcNow)
    {
        Attempts++;
        LastError = error.Length > 1000 ? error[..1000] : error;
        NextAttemptAt = utcNow.AddSeconds(Math.Min(Math.Pow(2, Attempts), 300));   // 2, 4, 8 ... tối đa 300 giây
        Touch();
    }
}