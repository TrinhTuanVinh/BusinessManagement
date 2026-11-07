namespace BusinessManagement.Application.Checkout.Payments;

public class BankRefund : IRefundablePayment
{
    public string PaymentMethod => "Bank";

    public void PaymentRefund(decimal amount)
    {
        Console.WriteLine($"Payment Refund Bank {amount}");
    }

}