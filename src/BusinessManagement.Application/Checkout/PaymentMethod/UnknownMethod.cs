public class UnknownMethod : IPaymentMethod
{
    public string PaymentMethod => "";
    public void Pay(decimal finalAmount)
    {
        throw new Exception("Unknown payment");
    }

    public void Refund()
    {
        throw new NotImplementedException();
    }
}