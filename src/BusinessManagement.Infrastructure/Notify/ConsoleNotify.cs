using BusinessManagement.Application.Checkout.Ports;

namespace BusinessManagement.Infrastructure.Notify;

public class ConsoleNotify : INotifier
{
    void INotifier.NotifyOrderProcessed(string email, string orderID, decimal finalAmount)
    {
    }

}