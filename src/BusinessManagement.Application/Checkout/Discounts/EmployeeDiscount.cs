namespace BusinessManagement.Application.Checkout.Discount;

public class EmployeeDiscountStrategy : IDiscountStrategy
{
    public int Priority => 10;

    public decimal CalculateDiscount(DiscountContext context) => Math.Round(context.OrderTotal * 0.20m, 2, MidpointRounding.AwayFromZero);

    public bool IsApplicable(DiscountContext context)
    {
        return context.CustomerType == "Employee";
    }
}