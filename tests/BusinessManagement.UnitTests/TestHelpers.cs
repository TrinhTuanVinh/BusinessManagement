public static class TestHelpers
{
    public static OrderProcessor CreateSut(
        IPaymentMethod payment, FakeOrderLog log, FakeNotifier notifier,
        params IDiscountStrategy[] strategies)
    {
        var publisher = new OrderEventPublisher(
         new IOrderProcessedHandler[] { new LogOrderHandler(log), new NotifyOrderHandler(notifier) },
         new ListLogger<OrderEventPublisher>());
        return CreateSut(payment, publisher, strategies);
    }
    public static OrderProcessor CreateSut(
    IPaymentMethod payment, OrderEventPublisher publisher,
    params IDiscountStrategy[] strategies)
    => new(new DiscountCalculator(strategies), new[] { payment },
           new InMemoryVoucherFactory(), publisher);

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
}