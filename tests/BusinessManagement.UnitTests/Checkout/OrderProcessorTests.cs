
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
    public void Process_InvalidVoucherCode_ThrowsBeforePayingAndTouchesNothing()
    {
        var cash = new FakePayment("Cash");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(cash, log, notifier, VipTenPercent());

        var ex = Assert.Throws<DomainException>(
            () => sut.Process(CreateOrder(100m, 1, "NOPE"), "VIP", "Cash", "a@b.com"));

        Assert.Contains("voucher code", ex.Message);
        Assert.Empty(cash.Paid);
        Assert.Empty(log.Records);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public void Process_VipWithSale50k_PaysAfterPercentThenFixed()
    {
        var cash = new FakePayment("Cash");
        var sut = TestHelpers.CreateSut(cash, new FakeOrderLog(), new FakeNotifier(), VipTenPercent());

        var final = sut.Process(CreateOrder(1_000_000m, 1, "SALE50K"), "VIP", "Cash", "a@b.com");

        Assert.Equal(850_000m, final);                 // 1.000.000 → 900.000 → 850.000
        Assert.Equal(new[] { 850_000m }, cash.Paid);
    }

    [Fact]
    public void Process_NoVoucher_DoesNotCallVoucherLogic()
    {
        var cash = new FakePayment("Cash");
        var sut = TestHelpers.CreateSut(cash, new FakeOrderLog(), new FakeNotifier(), VipTenPercent());

        var final = sut.Process(CreateOrder(100m), "VIP", "Cash", "a@b.com");

        Assert.Equal(90m, final);
    }

    [Fact]
    public void Process_VipCustomer_PaysTenPercentLess()
    {
        var cash = new FakePayment("Cash");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(cash, log, notifier, VipTenPercent());

        var final = sut.Process(CreateOrder(100m), "VIP", "Cash", "a@b.com");

        Assert.Equal(90m, final);
        Assert.Equal(new[] { 90m }, cash.Paid);
        Assert.Equal(90m, Assert.Single(log.Records).Amount);         // log ghi đúng số đã thu
        Assert.Equal(90m, Assert.Single(notifier.Sent).Amount);       // thông báo cũng vậy
    }

    [Theory]
    [InlineData("Gold")]
    [InlineData("Guest")]
    public void Process_CustomerTypeWithoutPolicy_PaysFullPrice(string customerType)
    {
        var cash = new FakePayment("Cash");
        var sut = TestHelpers.CreateSut(cash, new FakeOrderLog(), new FakeNotifier(), VipTenPercent());

        var final = sut.Process(CreateOrder(100m), customerType, "Cash", "a@b.com");

        Assert.Equal(100m, final);        // không ném lỗi, không giảm giá
        Assert.Equal(new[] { 100m }, cash.Paid);
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
    [Fact]
    public void Process_VipCashOrder_PaysDiscountedAmountAndRecordsOnce()
    {
        var cash = new FakePayment("Cash");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(cash, log, notifier);

        var final = sut.Process(CreateOrder(100m), "VIP", "Cash", "a@b.com");

        Assert.Equal(100m, final);
        Assert.Equal(new[] { 100m }, cash.Paid);
        Assert.Single(log.Records);
        Assert.Single(notifier.Sent);
    }

    [Fact]
    public void Process_UnsupportedPaymentMethod_ThrowsAndTouchesNothing()
    {
        var cash = new FakePayment("Cash");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(cash, log, notifier);

        var ex = Assert.Throws<DomainException>(
            () => sut.Process(CreateOrder(30m), "VIP", "Crypto", "a@c.com"));

        Assert.Contains("payment method", ex.Message);
        Assert.Empty(cash.Paid);
        Assert.Empty(log.Records);
        Assert.Empty(notifier.Sent);
    }

    [Theory]
    [InlineData("cash")]
    [InlineData("CASH")]
    public void Process_PaymentMethodWithDifferentCase_IsAccepted(string requested)
    {
        var cash = new FakePayment("Cash");               // đăng ký "Cash", gọi bằng chữ khác
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(cash, log, notifier);

        var final = sut.Process(CreateOrder(100m), "VIP", requested, "a@b.com");

        Assert.Equal(100m, final);
        Assert.Single(cash.Paid);
    }

    [Fact]
    public void Process_EmptyOrder_ThrowsDomainException()
    {
        // Given
        var bank = new FakePayment("Bank");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(bank, log, notifier);
        var ex = Assert.Throws<DomainException>(() => sut.Process(new Order(Guid.NewGuid()), "VIP", "Bank", "a@z.com"));
        Assert.Contains("at least one item", ex.Message);
        Assert.Empty(bank.Paid);
        Assert.Empty(log.Records);
        Assert.Empty(notifier.Sent);
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
    public void Process_WhenPaymentFails_DoesNotLogOrNotify()
    {
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(new ThrowingPayment(), log, notifier);

        Assert.Throws<InvalidOperationException>(
            () => sut.Process(CreateOrder(100m), "VIP", "Cash", "a@b.com"));

        Assert.Empty(log.Records);
        Assert.Empty(notifier.Sent);
    }
    [Fact]
    public void Refund_RefundableMethod_RefundsTheAmount()
    {
        var bank = new FakeRefundablePayment("Bank");
        var sut = new RefundProcessor(new[] { bank });

        sut.Refund("bank", 50m);                       // cũng kiểm tra không phân biệt hoa thường

        Assert.Equal(new[] { 50m }, bank.Refunded);
    }

    [Fact]
    public void Refund_MethodWithoutRefundSupport_ThrowsDomainException()
    {
        var sut = new RefundProcessor(new[] { new FakeRefundablePayment("Bank") });   // Cash không có trong danh sách

        var ex = Assert.Throws<DomainException>(() => sut.Refund("Cash", 50m));

        Assert.Contains("does not support refunds", ex.Message);
    }
}