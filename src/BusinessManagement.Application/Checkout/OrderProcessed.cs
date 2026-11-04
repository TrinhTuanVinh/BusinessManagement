public record OrderProcessed(Guid OrderId, decimal FinalAmount, string Email);

public interface IOrderProcessedHandler
{
    void Handle(OrderProcessed e);
}

public class LogOrderHandler : IOrderProcessedHandler
{
    private readonly IOrderLog _log;
    public LogOrderHandler(IOrderLog log) => _log = log;

    public void Handle(OrderProcessed e) => _log.Record(e.OrderId.ToString(), e.FinalAmount);
}

public class NotifyOrderHandler : IOrderProcessedHandler
{
    private readonly INotifier _notifier;
    public NotifyOrderHandler(INotifier notifier) => _notifier = notifier;

    public void Handle(OrderProcessed e)
        => _notifier.NotifyOrderProcessed(e.Email, e.OrderId.ToString(), e.FinalAmount);
}