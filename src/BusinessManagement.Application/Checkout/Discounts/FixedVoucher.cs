namespace BusinessManagement.Application.Checkout.Discount;

public class FixedVoucher : IDiscountStrategy
{
    private readonly string _code;
    private readonly decimal _amount;
    public FixedVoucher(int priority, string code, decimal amount)
        => (Priority, _code, _amount) = (priority, code, amount);

    public int Priority { get; }
    public bool IsApplicable(DiscountContext c)
        => string.Equals(c.VoucherCode, _code, StringComparison.OrdinalIgnoreCase);
    public decimal CalculateDiscount(DiscountContext c) => Math.Min(_amount, c.OrderTotal);
}
