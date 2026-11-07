using BusinessManagement.Application.Checkout.Discount;
using BusinessManagement.Domain.Common;

namespace BusinessManagement.Application.Checkout.Pricing;

public record PriceQuote(decimal OrderTotal, decimal Discount, decimal FinalAmount);
public class PricingService
{
    private readonly DiscountCalculator _calculator;
    private readonly IVoucherFactory _vouchers;

    public PricingService(DiscountCalculator calculator, IVoucherFactory vouchers)
    => (_calculator, _vouchers) = (calculator, vouchers);

    public PriceQuote Quote(decimal orderTotal, string customerType, string? voucherCode)
    {
        if (orderTotal <= 0)
        {
            throw new DomainException("Order total must be greater than 0.");
        }
        var type = customerType.Trim();
        var code = string.IsNullOrWhiteSpace(voucherCode) ? null : voucherCode.Trim().ToUpperInvariant();

        IDiscountStrategy? voucher = null;
        if (code is not null)
        {
            voucher = _vouchers.Create(code)
            ?? throw new DomainException($"Invalid voucher code '{voucherCode}'.");
        }

        var discount = _calculator.CalculateDiscount(new DiscountContext(type, code, orderTotal), voucher);
        return new PriceQuote(orderTotal, discount, orderTotal - discount);
    }
}