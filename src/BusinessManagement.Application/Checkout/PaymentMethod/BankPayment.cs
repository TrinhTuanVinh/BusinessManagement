public class BankPayment : IPaymentMethod, IRefundablePayment
{
    public string PaymentMethod => "Bank";
    public void Pay(decimal finalAmount)
    {
        Console.WriteLine($"Bank transfer requested: {finalAmount}");
    }

    public void PaymentRefund(decimal amount)
    {
        Console.WriteLine($"Payment Refund Bank{amount}");
    }


    public void Refund()
    {
        Console.WriteLine("Refund is completed!");
    }
}