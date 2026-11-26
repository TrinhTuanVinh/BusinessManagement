
using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.Domain.Common;

namespace BusinessManagement.Infrastructure.PaymentMethod;

public class LegacyBankGateway
{
    public virtual int Send(string accountNo, long amountInDong) => 0;
}
public class LegacyBankPaymentAdapter : IPaymentMethod
{
    private readonly LegacyBankGateway _gateway;
    private readonly string _merchantAccount;

    public LegacyBankPaymentAdapter(LegacyBankGateway gateway, string merchantAccount)
    {
        _gateway = gateway;
        _merchantAccount = merchantAccount;
    }

    public string PaymentMethod => "Bank";

    public void Pay(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainException("amount can not be negative");
        }
        if (amount == 0)
        {
            return;
        }
        long amountToSend = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        int result = _gateway.Send(_merchantAccount, amountToSend);
        if (result != 0)
        {
            throw new PaymentException($"Bank gateway returned error code {result}.", result);
        }
    }
}
