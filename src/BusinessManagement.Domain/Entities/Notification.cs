using BusinessManagement.Domain.Common;

namespace BusinessManagement.Domain.Entities;

public class Notification : BaseEntity
{
    private Notification() { }

    public Notification(Guid orderId, Guid customerId, string type, string message)
    {
        if (orderId == Guid.Empty) throw new DomainException("Order id is required.");
        if (string.IsNullOrWhiteSpace(type)) throw new DomainException("Notification type is required.");
        if (string.IsNullOrWhiteSpace(message)) throw new DomainException("Notification message is required.");
        OrderId = orderId; CustomerId = customerId; Type = type.Trim(); Message = message.Trim();
    }

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Type { get; private set; } = null!;
    public string Message { get; private set; } = null!;
}