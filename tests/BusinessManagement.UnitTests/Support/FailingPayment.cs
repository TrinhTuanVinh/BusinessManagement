using BusinessManagement.Application.Checkout.Payments;

namespace BusinessManagement.UnitTests.Support;
public class FailingPayment : IPaymentMethod
{
    public string PaymentMethod { get; }
    private readonly Exception _exception;
    public FailingPayment(string method, Exception exception) =>
    (PaymentMethod, _exception) = (method, exception);
    public void Pay(decimal finalAmount) => throw _exception;
}