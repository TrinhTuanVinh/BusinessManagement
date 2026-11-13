namespace BusinessManagement.UnitTests.Checkout;

using BusinessManagement.Application.Checkout.Events;
using BusinessManagement.Application.Checkout.Ports;
using BusinessManagement.UnitTests.Support;
using Microsoft.Extensions.Logging;

public class OrderEventPublisherTests
{
    private static readonly OrderProcessed Event = new(Guid.NewGuid(), 90m, "a@b.com");

    [Fact]
    public void Publish_CallsAllHandlersInRegistrationOrder()
    {
        var calls = new List<string>();
        var publisher = new OrderEventPublisher(
            new IOrderProcessedHandler[] { new SpyHandler("A", calls), new SpyHandler("B", calls) },
            new ListLogger<OrderEventPublisher>());

        publisher.Publish(Event);

        Assert.Equal(new[] { "A", "B" }, calls);
    }

    [Fact]
    public void Publish_FirstHandlerThrows_SecondStillRunsAndErrorIsLogged()
    {
        var calls = new List<string>();
        var logger = new ListLogger<OrderEventPublisher>();
        var boom = new InvalidOperationException("boom");
        var publisher = new OrderEventPublisher(
            new IOrderProcessedHandler[] { new SpyHandler("A", calls, boom), new SpyHandler("B", calls) },
            logger);

        publisher.Publish(Event);                       // không được ném

        Assert.Equal(new[] { "A", "B" }, calls);        // B vẫn chạy
        var error = Assert.Single(logger.Entries, x => x.Level == LogLevel.Error);
        Assert.Same(boom, error.Exception);
    }

    [Fact]
    public void LogOrderHandler_RecordsOrderIdAndAmount()
    {
        var log = new FakeOrderLog();
        var id = Guid.NewGuid();

        new LogOrderHandler(log).Handle(new OrderProcessed(id, 90m, "a@b.com"));

        Assert.Equal((id.ToString(), 90m), Assert.Single(log.Records));
    }

    [Fact]
    public void NotifyOrderHandler_SendsEmailWithOrderIdAndAmount()
    {
        var notifier = new FakeNotifier();
        var id = Guid.NewGuid();

        new NotifyOrderHandler(notifier).Handle(new OrderProcessed(id, 90m, "a@b.com"));

        Assert.Equal(("a@b.com", id.ToString(), 90m), Assert.Single(notifier.Sent));
    }
  
}