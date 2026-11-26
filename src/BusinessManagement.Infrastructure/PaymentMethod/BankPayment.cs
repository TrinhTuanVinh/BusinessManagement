namespace BusinessManagement.Infrastructure.PaymentMethod;

using BusinessManagement.Application.Checkout.Payments;

public class BankPayment : IPaymentMethod, IRefundablePayment
{
    public string PaymentMethod => "Bank";
    public void Pay(decimal finalAmount)
    {
    }

    public void PaymentRefund(decimal amount)
    {
    }

}