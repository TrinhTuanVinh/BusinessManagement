public class LegacyBankGatewayTests
{
    private class FakeGateway : LegacyBankGateway
    {
        public int ReturnCode { get; set; } = 0;
        public List<(string Account, long Amount)> Calls { get; } = new();

        public override int Send(string accountNo, long amountInDong)
        {
            Calls.Add((accountNo, amountInDong));
            return ReturnCode;
        }
    }
    [Fact]
    public void Pay_ValidAmount_SendsRoundedAmountToMerchantAccount()
    {
        // Given
        FakeGateway gateway = new FakeGateway();
        var merchantAcount = "ACC";
        var sut = new LegacyBankPaymentAdapter(gateway, merchantAcount);
        decimal amount = 850_000m;

        // When
        sut.Pay(amount);
        // Then
        Assert.Single(gateway.Calls);
        Assert.Equal("ACC", gateway.Calls[0].Account);
        Assert.Equal(850_000L, gateway.Calls[0].Amount);
    }

    [Theory]
    [InlineData(100.4, 100)]
    [InlineData(100.5, 101)]
    public void Pay_FractionalAmount_RoundsAwayFromZero(double amount, long expected)
    {
        // Given
        FakeGateway gateway = new FakeGateway();
        var merchantAcount = "123456789";
        var sut = new LegacyBankPaymentAdapter(gateway, merchantAcount);

        // When
        sut.Pay((decimal)amount);
        // Then
        Assert.Equal(expected, gateway.Calls[0].Amount);
    }

    [Fact]
    public void Pay_GatewayReturnsError_ThrowsPaymentExceptionWithCode()
    {
        // Given
        FakeGateway gateway = new FakeGateway() { ReturnCode = 7 };
        var sut = new LegacyBankPaymentAdapter(gateway, "ACCC");
        // When
        var err = Assert.Throws<PaymentException>(() => sut.Pay(100m));
        // Then
        Assert.Equal(7, err.ErrorCode);
    }

    [Fact]
    public void Pay_NegativeAmount_ThrowsAndDoesNotCallGateway()
    {
        // Given
        FakeGateway gateway = new FakeGateway();
        var sut = new LegacyBankPaymentAdapter(gateway, "ACCC");
        // When
        var ex = Assert.Throws<DomainException>(() => sut.Pay(-1m));
        // Then
        Assert.Empty(gateway.Calls);
        Assert.Contains("amount", ex.Message);
    }

    [Fact]
    public void Pay_ZeroAmount_DoesNotCallGateway()
    {
        // Given
        FakeGateway gateway = new FakeGateway();
        var sut = new LegacyBankPaymentAdapter(gateway, "ACCC");
        // When
        sut.Pay(0);
        // Then
        Assert.Empty(gateway.Calls);
    }

    [Fact]
    public void PaymentMethod_IsBank()
    {
        // Given
        FakeGateway gateway = new FakeGateway();
        var sut = new LegacyBankPaymentAdapter(gateway, "ACCC");
        // When
        // Then
        Assert.Equal("Bank", sut.PaymentMethod);
    }

    [Fact]
    public void Process_VoucherCoversWholeOrder_DoesNotCallGatewayButStillLogs()
    {
        var gateway = new FakeGateway();
        var log = new FakeOrderLog();
        var sut = TestHelpers.CreateSut(new LegacyBankPaymentAdapter(gateway, "ACC"), log, new FakeNotifier());

        // đơn 30.000 + voucher cố định 50.000 → thanh toán 0
        sut.Process(TestHelpers.CreateOrder(30_000m, 1, "SALE50K"), "VIP", "Bank", "a@c.com");

        Assert.Empty(gateway.Calls);
        Assert.Single(log.Records);
    }
    [Fact]
    public void Process_BankPayment_SendsOrderAmountToGateway()
    {
        // Given
        FakeGateway gateway = new FakeGateway();
        var bankPaymentAdapter = new LegacyBankPaymentAdapter(gateway, "ABBB");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(bankPaymentAdapter, log, notifier);
        // When
        sut.Process(OrderProcessorTests.CreateOrder(100m), "VIP", "Bank", "a@c.com");
        // Then
        Assert.Single(gateway.Calls);
        Assert.Equal("ABBB", gateway.Calls[0].Account);
        Assert.Equal(100L, gateway.Calls[0].Amount);
    }


    [Fact]
    public void PayOrderProcess_GatewayReturnsError_ThrowsPaymentExceptionAndDoesNotLogOrNotify()
    {
        // Given
        FakeGateway gateway = new FakeGateway() { ReturnCode = 7 };
        var bankPaymentAdapter = new LegacyBankPaymentAdapter(gateway, "ABCD");
        var log = new FakeOrderLog();
        var notifier = new FakeNotifier();
        var sut = TestHelpers.CreateSut(bankPaymentAdapter, log, notifier);
        // When
        var ex = Assert.Throws<PaymentException>(() => sut.Process(OrderProcessorTests.CreateOrder(100m), "VIP", "Bank", "a@c.com"));
        // Then
        Assert.Equal(7, ex.ErrorCode);
        Assert.Empty(log.Records);
        Assert.Empty(notifier.Sent);
    }

}