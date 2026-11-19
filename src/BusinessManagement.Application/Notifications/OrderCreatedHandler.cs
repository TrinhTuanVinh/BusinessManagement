using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BusinessManagement.Application.Notifications;

public interface IOrderCreatedHandler
{
    Task HandleAsync(OrderCreated message, CancellationToken ct);
}

public class OrderCreatedHandler(IAppDbContext db, ILogger<OrderCreatedHandler> logger) : IOrderCreatedHandler
{
    public async Task HandleAsync(OrderCreated message, CancellationToken ct)
    {
        db.Notifications.Add(new Notification(message.OrderId, message.CustomerId, "OrderCreated",
            $"Order {message.OrderId} created, total {message.Total}"));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Notification created for order {OrderId} from event {EventId}", message.OrderId, message.EventId);
    }
}