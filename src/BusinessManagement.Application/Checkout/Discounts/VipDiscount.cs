namespace BusinessManagement.Application.Checkout.Discount;

public class VipDiscountStrategy : IDiscountStrategy
{
    public int Priority => 15;

    public decimal CalculateDiscount(DiscountContext context) => Math.Round(context.OrderTotal * 0.10m, 2, MidpointRounding.AwayFromZero);

    public bool IsApplicable(DiscountContext context)
    {
        return context.CustomerType == "VIP";
    }
}