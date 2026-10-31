public class FixedVoucherDiscount : IDiscountPolicy
{
    private readonly decimal _amount;
    public FixedVoucherDiscount(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("");
        _amount = amount;
    }
    public string CustomerType => "FixedVoucher";

    public decimal Calculate(decimal orderTotal) => Math.Min(_amount, Math.Max(0, orderTotal));
}