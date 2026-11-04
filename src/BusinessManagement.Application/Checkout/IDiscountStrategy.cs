public record DiscountContext(string CustomerType, string? VoucherCode, decimal OrderTotal);

public interface IDiscountStrategy
{
    int Priority { get; }
    bool IsApplicable(DiscountContext context);
    decimal CalculateDiscount(DiscountContext context);
}

public class DiscountCalculator
{
    private readonly List<IDiscountStrategy> _strategies;
    public DiscountCalculator(IEnumerable<IDiscountStrategy> strategies)
    {
        _strategies = strategies.OrderBy(s => s.Priority).ToList();
    }

    public decimal CalculateDiscount(DiscountContext context, IDiscountStrategy? voucher = null)
    {
        var strategies = voucher is null
        ? _strategies
        : _strategies.Append(voucher).OrderBy(v => v.Priority).ToList();
        decimal totalDiscount = 0;
        decimal currentTotal = context.OrderTotal;
        foreach (var strategy in strategies)
        {
            if (!strategy.IsApplicable(context)) continue;
            decimal discount = strategy.CalculateDiscount(context with { OrderTotal = currentTotal });
            if (discount < 0)
            {
                throw new DomainException($"{strategy.GetType().Name} returned a negative discount.");
            }
            discount = Math.Min(discount, currentTotal);
            currentTotal -= discount;
            totalDiscount += discount;
        }
        return totalDiscount;
    }
}