public class PercentDiscount : IDiscountStrategy
{
    private readonly Func<DiscountContext, bool> _applies;
    private readonly decimal _rate;
    public PercentDiscount(int priority, decimal rate, Func<DiscountContext, bool> applies)
        => (Priority, _rate, _applies) = (priority, rate, applies);

    public int Priority { get; }
    public bool IsApplicable(DiscountContext c) => _applies(c);
    public decimal CalculateDiscount(DiscountContext c)
        => Math.Round(c.OrderTotal * _rate, 2, MidpointRounding.AwayFromZero);
}

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
