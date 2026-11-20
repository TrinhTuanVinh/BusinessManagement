using BusinessManagement.Domain.Common;

namespace BusinessManagement.Domain.Entities;
public class ProcessedMessage : BaseEntity
{
    private ProcessedMessage() { }

    public ProcessedMessage(Guid eventId, string consumer)
    {
        if (eventId == Guid.Empty) throw new DomainException("Event id is required.");
        if (string.IsNullOrWhiteSpace(consumer)) throw new DomainException("Consumer is required.");
        EventId = eventId; Consumer = consumer.Trim();
    }

    public Guid EventId { get; private set; }
    public string Consumer { get; private set; } = null!;
}