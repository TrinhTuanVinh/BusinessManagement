public class VipDiscount : IDiscountPolicy
{
    public string CustomerType => "VIP";
    public decimal Calculate(decimal orderTotal) =>  Math.Round(orderTotal * 0.10m, 2, MidpointRounding.AwayFromZero);
}