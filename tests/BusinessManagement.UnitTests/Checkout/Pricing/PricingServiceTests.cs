using BusinessManagement.Application.Checkout.Discount;
using BusinessManagement.Application.Checkout.Pricing;
using BusinessManagement.Domain.Common;

public class PricingServiceTests
{
    private static PricingService CreateSut() => new(
        new DiscountCalculator(new IDiscountStrategy[]
        {
            new PercentDiscount(1, 0.10m, c => c.CustomerType.Equals("VIP", StringComparison.OrdinalIgnoreCase)),
            new PercentDiscount(1, 0.05m, c => c.CustomerType.Equals("Student", StringComparison.OrdinalIgnoreCase)),
        }),
        new InMemoryVoucherFactory());

    [Fact]
    public void Quote_VipWithoutVoucher_AppliesTenPercent()
    {
        var q = CreateSut().Quote(1_000_000m, "VIP", null);

        Assert.Equal(new PriceQuote(1_000_000m, 100_000m, 900_000m), q);
    }

    [Fact]
    public void Quote_VipWithSale50k_AppliesPercentThenVoucher()
    {
        var q = CreateSut().Quote(1_000_000m, "VIP", "SALE50K");

        Assert.Equal(150_000m, q.Discount);
        Assert.Equal(850_000m, q.FinalAmount);
    }

    [Theory]
    [InlineData(" sale50k ")]
    [InlineData("Sale50K")]
    public void Quote_VoucherWithSpacesOrCase_StillApplied(string code)
        => Assert.Equal(150_000m, CreateSut().Quote(1_000_000m, "VIP", code).Discount);

    [Fact]
    public void Quote_CustomerTypeWithSpacesAndCase_StillMatched()
        => Assert.Equal(100_000m, CreateSut().Quote(1_000_000m, " vip ", null).Discount);

    [Theory]
    [InlineData("Gold")]
    [InlineData("Guest")]
    public void Quote_CustomerTypeWithoutPolicy_NoDiscount(string type)
        => Assert.Equal(0m, CreateSut().Quote(500_000m, type, null).Discount);

    [Fact]
    public void Quote_VoucherLargerThanOrder_CapsAtOrderTotal()
    {
        var q = CreateSut().Quote(30_000m, "Gold", "SALE50K");

        Assert.Equal(30_000m, q.Discount);
        Assert.Equal(0m, q.FinalAmount);
    }

    [Fact]
    public void Quote_InvalidVoucher_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => CreateSut().Quote(100m, "VIP", "NOPE"));

        Assert.Contains("NOPE", ex.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Quote_NonPositiveTotal_Throws(string total)
        => Assert.Throws<DomainException>(() => CreateSut().Quote(decimal.Parse(total), "VIP", null));
}