using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessManagement.Application.Notifications;

public interface IOrderCreatedHandler
{
    Task HandleAsync(OrderCreated message, CancellationToken ct);
}

public class OrderCreatedHandler(IAppDbContext db, ILogger<OrderCreatedHandler> logger) : IOrderCreatedHandler
{
    public const string ConsumerName = "OrderCreatedHandler";
    public async Task HandleAsync(OrderCreated message, CancellationToken ct)
    {
        if (await AlreadyProcessedAsync(message.EventId, ct)) { LogDuplicate(message); return; }

        db.ProcessedMessages.Add(new ProcessedMessage(message.EventId, ConsumerName));
        db.Notifications.Add(new Notification(message.OrderId, message.CustomerId, "OrderCreated",
            $"Order {message.OrderId} created, total {message.Total}"));
        try
        {
            await db.SaveChangesAsync(ct);            // một transaction: dấu và thông báo cùng thành công hoặc cùng thất bại
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await AlreadyProcessedAsync(message.EventId, ct)) { LogDuplicate(message); return; }   // thua cuộc đua song song
            throw;
        }
        logger.LogInformation("Notification created for order {OrderId} from event {EventId}", message.OrderId, message.EventId);
    }

    private Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct)
        => db.ProcessedMessages.AsNoTracking().AnyAsync(p => p.Consumer == ConsumerName && p.EventId == eventId, ct);

    private void LogDuplicate(OrderCreated m)
        => logger.LogInformation("Duplicate event {EventId} for order {OrderId} ignored", m.EventId, m.OrderId);
}