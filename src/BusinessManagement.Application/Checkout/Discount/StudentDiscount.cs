public class StudentDiscount : IDiscountPolicy
{
    public string CustomerType => "Student";
    public decimal Calculate(decimal orderTotal) => Math.Round(orderTotal * 0.05m, 2, MidpointRounding.AwayFromZero);
}