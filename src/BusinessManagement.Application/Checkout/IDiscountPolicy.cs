// Application/Checkout/IDiscountPolicy.cs
public interface IDiscountPolicy
{
    string CustomerType { get; }
    /// Trả về số tiền giảm, trong khoảng [0, orderTotal].
    decimal Calculate(decimal orderTotal);
}