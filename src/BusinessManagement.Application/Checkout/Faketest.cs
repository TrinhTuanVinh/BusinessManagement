public class FakePayment : IPaymentMethod
{
    public FakePayment(string method) => PaymentMethod = method;
    public string PaymentMethod { get; }
    public List<decimal> Paid { get; } = new();

    public void Pay(decimal amount) => Paid.Add(amount);

}
public class ThrowingPayment : IPaymentMethod
{
    public string PaymentMethod => "Cash";
    public void Pay(decimal amount) => throw new InvalidOperationException("Gateway down");
}

public class FakeOrderLog : IOrderLog
{
    public List<(string OrderId, decimal Amount)> Records { get; } = new();
    public void Record(string orderId, decimal amount) => Records.Add((orderId, amount));
}

public class FakeNotifier : INotifier
{
    public List<(string Email, string OrderId, decimal Amount)> Sent { get; } = new();
    public void NotifyOrderProcessed(string email, string orderId, decimal amount)
        => Sent.Add((email, orderId, amount));
}