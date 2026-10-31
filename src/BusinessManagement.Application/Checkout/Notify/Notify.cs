public class Notify : INotifier
{
    void INotifier.NotifyOrderProcessed(string email, string orderID, decimal finalAmount)
    {
        Console.WriteLine($"Email to {email}: order {orderID}, total {finalAmount}");
    }

}