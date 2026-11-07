namespace BusinessManagement.Application.Checkout.Discount;

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
