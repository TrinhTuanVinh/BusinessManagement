namespace BusinessManagement.Application.Checkout.Payments;

public class PaymentException : Exception
{
    public int ErrorCode { get; }
    public PaymentException(string message, int errorCode = 0) : base(message) => ErrorCode = errorCode;
}