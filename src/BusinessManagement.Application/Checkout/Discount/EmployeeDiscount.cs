public class EmployeeDiscount : IDiscountPolicy
{
    public string CustomerType => "Employee";
    public decimal Calculate(decimal orderTotal) => Math.Round(orderTotal * 0.20m, 2, MidpointRounding.AwayFromZero);
}

public class EmployeeDiscountStrategy : IDiscountStrategy
{
    public int Priority => 10;

    public decimal CalculateDiscount(DiscountContext context) => Math.Round(context.OrderTotal * 0.20m, 2, MidpointRounding.AwayFromZero);

    public bool IsApplicable(DiscountContext context)
    {
        return context.CustomerType == "Employee";
    }
}