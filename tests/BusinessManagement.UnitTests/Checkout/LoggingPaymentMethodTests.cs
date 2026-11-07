using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.UnitTests.Support;
using Microsoft.Extensions.Logging;

namespace BusinessManagement.UnitTests.Checkout;

public class LoggingPaymentMethodTests
{
    private static (LoggingPaymentMethod Sut, ListLogger<LoggingPaymentMethod> Logger)
    Create(IPaymentMethod inner)
    {
        var logger = new ListLogger<LoggingPaymentMethod>();
        return (new LoggingPaymentMethod(inner, logger), logger);
    }
    [Fact]
    public void Pay_Success_CallsInnerOnceWithSameAmount()
    {
        // Given
        var inner = new FakePayment("Cash");
        var (sut, _) = Create(inner);
        // When
        sut.Pay(850_00m);

        // Then
        Assert.Equal(new[] { 850_00m }, inner.Paid);
    }

    [Fact]
    public void PaymentMethod_ReturnsKeyOfInner()
    {
        // Given
        var inner = new FakePayment("Bank");
        var (sut, _) = Create(inner);
        // When

        // Then
        Assert.Equal("Bank", sut.PaymentMethod);
    }

    [Fact]
    public void Pay_Success_LogsAmountAndMethodAndNoError()
    {
        // Given
        var (sut, logger) = Create(new FakePayment("Bank"));
        // When
        sut.Pay(850_000m);
        // Then
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Information
            && Equals(e.Values["Amount"], 850_000m)
            && Equals(e.Values["Method"], "Bank"));
    }

    [Fact]
    public void Pay_InnerThrows_RethrowsSameExceptionAndLogsErrorOnce()
    {
        var original = new InvalidOperationException("Gateway down");
        var (sut, logger) = Create(new FailingPayment("Bank", original));

        var thrown = Assert.Throws<InvalidOperationException>(() => sut.Pay(100m));

        Assert.Same(original, thrown);                                   // ném lại nguyên đối tượng
        var error = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Same(original, error.Exception);                          // log giữ được exception
    }

    [Fact]
    public void Pay_ExceptionMessageContainsBraces_StillRethrowsOriginal()
    {
        var original = new InvalidOperationException("bad {input");
        var (sut, _) = Create(new FailingPayment("Bank", original));

        var thrown = Assert.Throws<InvalidOperationException>(() => sut.Pay(100m));

        Assert.Same(original, thrown);
    }

    [Fact]
    public void Process_WithLoggingDecorator_StillPaysThroughInner()
    {
        var inner = new FakePayment("Bank");
        var (decorated, _) = Create(inner);
        var sut = TestHelpers.CreateSut(decorated, new FakeOrderLog(), new FakeNotifier());

        sut.Process(TestHelpers.CreateOrder(100m), "VIP", "Bank", "a@c.com");

        Assert.Equal(new[] { 100m }, inner.Paid);
    }
}