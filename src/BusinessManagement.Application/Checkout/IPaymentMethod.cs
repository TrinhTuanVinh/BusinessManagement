public interface IPaymentMethod
{
    string PaymentMethod { get; }
    void Pay(decimal finalAmount);
}