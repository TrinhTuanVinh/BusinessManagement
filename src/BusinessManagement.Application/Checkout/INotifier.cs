public interface INotifier
{
    void NotifyOrderProcessed(string email, string orderID, decimal finalAmount);
}