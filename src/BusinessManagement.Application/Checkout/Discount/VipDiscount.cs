public class VipDiscount : IDiscountPolicy
{
    public string CustomerType => "VIP";
    public decimal Calculate(decimal orderTotal) =>  Math.Round(orderTotal * 0.10m, 2, MidpointRounding.AwayFromZero);
}


public class VipDiscountStrategy : IDiscountStrategy
{
    public int Priority => 15;

    public decimal CalculateDiscount(DiscountContext context) => Math.Round(context.OrderTotal * 0.10m, 2, MidpointRounding.AwayFromZero);

    public bool IsApplicable(DiscountContext context)
    {
        return context.CustomerType == "VIP";
    }
}