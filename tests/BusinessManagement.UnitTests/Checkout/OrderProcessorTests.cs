
public class OrderProcessorTests
{

    private static OrderProcessor CreateSut(
        IPaymentMethod payment, FakeOrderLog log, FakeNotifier notifier,
        params IDiscountPolicy[] discounts)
        => new(discounts.Length > 0 ? discounts : new IDiscountPolicy[] { new VipDiscount() },
               new[] { payment }, notifier, log);

    private static Order CreateOrder(decimal price = 100m, int quantity = 1)
    {
        var order = new Order(Guid.NewGuid());
        order.AddItem(new Product("Wood 1", "Wood", price, 100), quantity);
        return order;
    }

    [Fact]
    public void Process_VipCashOrder_PaysDiscountedAmountAndRecordsOnce()
    {
        var cash = new FakePayment("Cash");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = CreateSut(cash, log, notifier);

        var final = sut.Process(CreateOrder(100m), "VIP", "Cash", "a@b.com");

        Assert.Equal(90m, final);
        Assert.Equal(new[] { 90m }, cash.Paid);
        Assert.Single(log.Records);
        Assert.Single(notifier.Sent);
    }

    [Fact]
    public void Process_UnsupportedPaymentMethod_ThrowsAndTouchesNothing()
    {
        var cash = new FakePayment("Cash");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = CreateSut(cash, log, notifier);

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
        var sut = CreateSut(cash, log, notifier);

        var final = sut.Process(CreateOrder(100m), "VIP", requested, "a@b.com");

        Assert.Equal(90m, final);
        Assert.Single(cash.Paid);
    }
    [Fact]
    public void Process_UnsupportedCustomerType_ThrowsBeforePaying()
    {
        // Given
        var bank = new FakePayment("Bank");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = CreateSut(bank, log, notifier);
        var ex = Assert.Throws<DomainException>(() => sut.Process(CreateOrder(30m), "Gold", "Bank", "a@l.com"));

        Assert.Contains("customer type", ex.Message);
        Assert.Empty(bank.Paid);
        Assert.Empty(log.Records);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public void Process_EmptyOrder_ThrowsDomainException()
    {
        // Given
        var bank = new FakePayment("Bank");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = CreateSut(bank, log, notifier);
        var ex = Assert.Throws<DomainException>(() => sut.Process(new Order(Guid.NewGuid()), "VIP", "Bank", "a@z.com"));
        Assert.Contains("at least one item", ex.Message);
        Assert.Empty(bank.Paid);
        Assert.Empty(log.Records);
        Assert.Empty(notifier.Sent);
    }

    public class TestDiscount : IDiscountPolicy
    {
        public string CustomerType => "Test";

        public decimal Calculate(decimal orderTotal) => Math.Round(orderTotal * 0.05m, 2, MidpointRounding.AwayFromZero);
    }

    [Fact]
    public void Process_NewDiscountPolicy_IsAppliedWithoutChangingProcessor()
    {
        var bank = new FakePayment("Bank");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = CreateSut(bank, log, notifier, new TestDiscount());

        var final = sut.Process(CreateOrder(100m), "Test", "Bank", "a@b.com");

        Assert.Equal(95m, final);
        Assert.Equal(new[] { 95m }, bank.Paid);
    }

    [Fact]
    public void Process_WhenPaymentFails_DoesNotLogOrNotify()
    {
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = CreateSut(new ThrowingPayment(), log, notifier);

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