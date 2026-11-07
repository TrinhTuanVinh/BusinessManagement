namespace BusinessManagement.Application.Checkout.Events;

public interface IOrderProcessedHandler
{
    void Handle(OrderProcessed e);
}
