using BusinessManagement.Application.Checkout.Payments;

namespace BusinessManagement.Infrastructure.PaymentMethod;


public class CashMethod : IPaymentMethod
{
    public string PaymentMethod => "Cash";
    public void Pay(decimal finalAmount)
    {
    }
}