public interface IRefundablePayment
{
    string PaymentMethod { get; }
    void PaymentRefund(decimal amount);

}

public class RefundProcessor
{
    private readonly IReadOnlyDictionary<string, IRefundablePayment> _refunds;
    public RefundProcessor(IEnumerable<IRefundablePayment> refunds) => _refunds = refunds.ToDictionary(
        m => m.PaymentMethod, StringComparer.OrdinalIgnoreCase);
    public void Refund(string paymentMethod, decimal amount)
    {
        if (!_refunds.TryGetValue(paymentMethod, out var refundMethod))
        {
            throw new DomainException($"Payment method '{paymentMethod}' does not support refunds.");
        }
        refundMethod.PaymentRefund(amount);
    }
}

// Fake dùng trong test
public class FakeRefundablePayment : IRefundablePayment
{
    public FakeRefundablePayment(string method) => PaymentMethod = method;
    public string PaymentMethod { get; }
    public List<decimal> Refunded { get; } = new();
    public void PaymentRefund(decimal amount) => Refunded.Add(amount);
}