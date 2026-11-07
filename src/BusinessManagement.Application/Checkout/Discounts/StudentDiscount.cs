namespace BusinessManagement.Application.Checkout.Discount;

public class StudentDiscountStrategy : IDiscountStrategy
{
    public int Priority => 5;

    public decimal CalculateDiscount(DiscountContext context) => Math.Round(context.OrderTotal * 0.05m, 2, MidpointRounding.AwayFromZero);

    public bool IsApplicable(DiscountContext context)
    {
        return context.CustomerType == "Student";
    }
}