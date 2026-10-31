public class EmployeeDiscount : IDiscountPolicy
{
    public string CustomerType => "Employee";
    public decimal Calculate(decimal orderTotal) => Math.Round(orderTotal * 0.20m, 2, MidpointRounding.AwayFromZero);
}