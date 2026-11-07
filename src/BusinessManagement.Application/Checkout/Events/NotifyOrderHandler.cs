using BusinessManagement.Application.Checkout.Ports;

namespace BusinessManagement.Application.Checkout.Events;


public class NotifyOrderHandler : IOrderProcessedHandler
{
    private readonly INotifier _notifier;
    public NotifyOrderHandler(INotifier notifier) => _notifier = notifier;

    public void Handle(OrderProcessed e)
        => _notifier.NotifyOrderProcessed(e.Email, e.OrderId.ToString(), e.FinalAmount);
}