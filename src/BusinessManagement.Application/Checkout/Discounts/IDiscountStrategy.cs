namespace BusinessManagement.Application.Checkout.Discount;

public interface IDiscountStrategy
{
    int Priority { get; }
    bool IsApplicable(DiscountContext context);
    decimal CalculateDiscount(DiscountContext context);
}
