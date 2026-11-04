public interface IPaymentMethod
{
    string PaymentMethod { get; }
    void Pay(decimal finalAmount);
}

public class PaymentException : Exception
{
    public int ErrorCode { get; }
    public PaymentException(string message, int errorCode = 0) : base(message) => ErrorCode = errorCode;
}