
using BusinessManagement.Application.Checkout.Discount;
using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;
using BusinessManagement.UnitTests.Support;

namespace BusinessManagement.UnitTests.Checkout;

public class OrderProcessorTests
{

    private static IDiscountStrategy VipTenPercent()
        => new PercentDiscount(1, 0.10m,
            c => c.CustomerType.Equals("VIP", StringComparison.OrdinalIgnoreCase));
    public static Order CreateOrder(decimal price = 100m, int quantity = 1,
        string? voucherCode = null)
    {
        var order = new Order(Guid.NewGuid());
        order.AddItem(new Product("Wood 1", "Wood", price, 100), quantity);
        if (voucherCode != null)
        {
            order.ApplyVoucher(voucherCode);
        }
        return order;
    }


    [Fact]
    public void CalculateDiscount_PercentageBeforeFixed_Returns150000()
    {
        // Arrange
        var strategies = new IDiscountStrategy[]
        {
            new PercentDiscount(1, 0.10m, c => c.CustomerType.Equals("VIP", StringComparison.OrdinalIgnoreCase)),
            new FixedVoucher(2, "SALE50K", 50_000m),
        };
        var calc = new DiscountCalculator(strategies);

        Assert.Equal(150_000m, calc.CalculateDiscount(new("VIP", "SALE50K", 1_000_000m)));
    }
    [Fact]
    public void CalculateDiscount_FixedBeforePercentage_Returns145000()
    {
        // Arrange
        var strategies = new IDiscountStrategy[]
        {
             new FixedVoucher(1, "SALE50K", 50_000m),
            new PercentDiscount(2, 0.10m, c => c.CustomerType.Equals("VIP", StringComparison.OrdinalIgnoreCase)),

        };
        var calc = new DiscountCalculator(strategies);

        Assert.Equal(145_000m, calc.CalculateDiscount(new("VIP", "SALE50K", 1_000_000m)));
    }
    [Fact]
    public void CalculateDiscount_VoucherHigherTotal__CapsAtOrderTotal()
    {
        // Arrange
        var strategies = new IDiscountStrategy[]
        {
             new FixedVoucher(1, "SALE50K", 50_000m),

        };
        var calc = new DiscountCalculator(strategies);

        Assert.Equal(30_000m, calc.CalculateDiscount(new("VIP", "SALE50K", 30_000m)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("OTHER")]
    public void CalculateDiscount_NoMatchingVoucherCode_ReturnsZero(string? voucherCode)
    {
        var calc = new DiscountCalculator(new IDiscountStrategy[]
            { new FixedVoucher(1, "SALE50K", 50_000m) });

        Assert.Equal(0m, calc.CalculateDiscount(new("VIP", voucherCode, 50_000m)));
    }

    [Theory]
    [InlineData("sale50k")]
    [InlineData("SALE50K")]
    [InlineData("Sale50K")]
    public void CalculateDiscount_VoucherCodeIsCaseInsensitive_AppliesDiscount(string voucherCode)
    {
        var calc = new DiscountCalculator(new IDiscountStrategy[]
            { new FixedVoucher(1, "SALE50K", 50_000m) });

        Assert.Equal(50_000m, calc.CalculateDiscount(new("VIP", voucherCode, 100_000m)));
    }
    [Fact]
    public void CalculateDiscount_NegativeAmount_ThrowsDomainException()
    {
        // Arrange
        var strategies = new IDiscountStrategy[]
        {
             new FixedVoucher(1, "SALE50K", -50_000m),
        };
        var cal = new DiscountCalculator(strategies);
        Assert.Throws<DomainException>(() => cal.CalculateDiscount(new("VIP", "SALE50K", 100_000m)));
    }

    private class StubStrategy : IDiscountStrategy
    {
        private readonly Func<decimal, decimal> _calc;
        public StubStrategy(int priority, Func<decimal, decimal> calc) => (Priority, _calc) = (priority, calc);
        public int Priority { get; }
        public bool IsApplicable(DiscountContext c) => true;
        public decimal CalculateDiscount(DiscountContext c) => _calc(c.OrderTotal);
    }

    [Fact]
    public void Calculate_StrategyReturnsMoreThanRemaining_CalculatorCapsIt()
    {
        var calc = new DiscountCalculator(new IDiscountStrategy[] { new StubStrategy(1, _ => 50_000m) });

        Assert.Equal(30_000m, calc.CalculateDiscount(new("VIP", null, 30_000m)));
    }

    [Fact]
    public void Calculate_WithVoucherParameter_AppliesAfterCustomerDiscount()
    {
        var calc = new DiscountCalculator(new IDiscountStrategy[]
            { new PercentDiscount(1, 0.10m, c => c.CustomerType == "VIP") });
        var voucher = new FixedVoucher(2, "SALE50K", 50_000m);

        var discount = calc.CalculateDiscount(new("VIP", "SALE50K", 1_000_000m), voucher);

        Assert.Equal(150_000m, discount);
    }

    public class TestDiscount : IDiscountStrategy
    {
        public int Priority => 1;

        public decimal CalculateDiscount(DiscountContext context) => Math.Round(context.OrderTotal * 0.05m, 2, MidpointRounding.AwayFromZero);
        public bool IsApplicable(DiscountContext context)
        {
            return context.CustomerType == "Test";
        }
    }


    [Fact]
    public void Refund_RefundableMethod_RefundsTheAmount()
    {
        var bank = new FakeRefundablePayment("Bank");
        var sut = new RefundProcessor(new[] { bank });

        sut.Refund("bank", 50m);

        Assert.Equal(new[] { 50m }, bank.Refunded);
    }

    [Fact]
    public void Refund_MethodWithoutRefundSupport_ThrowsDomainException()
    {
        var sut = new RefundProcessor(new[] { new FakeRefundablePayment("Bank") });

        var ex = Assert.Throws<DomainException>(() => sut.Refund("Cash", 50m));

        Assert.Contains("does not support refunds", ex.Message);
    }
}