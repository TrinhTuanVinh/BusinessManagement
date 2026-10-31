
public class OrderProcessor
{
    private readonly IReadOnlyDictionary<string, IDiscountPolicy> _discounts;
    private readonly IReadOnlyDictionary<string, IPaymentMethod> _payments;
    private readonly INotifier _notifier;
    private readonly IOrderLog _orderLog;

    public OrderProcessor(IEnumerable<IDiscountPolicy> discounts, IEnumerable<IPaymentMethod> payments, INotifier notifier, IOrderLog orderLog)
    {
        _discounts = discounts.ToDictionary(d => d.CustomerType, StringComparer.OrdinalIgnoreCase);
        _payments = payments.ToDictionary(p => p.PaymentMethod, StringComparer.OrdinalIgnoreCase);
        _notifier = notifier;
        _orderLog = orderLog;
    }
    public decimal Process(Order order, string customerType, string paymentMethod, string email)
    {
        // 1. validate
        if (order.Items.Count == 0) throw new DomainException("Order must contain at least one item.");

        // 2. discount
        if (!_discounts.TryGetValue(customerType, out var discountPolicy))
        {
            throw new DomainException($"Unsupported customer type '{customerType}'.");
        }
        decimal discount = discountPolicy.Calculate(order.Total);
        var finalAmount = order.Total - discount;

        // 3. payment
        if (!_payments.TryGetValue(paymentMethod, out var paymentMethodService))
        {
            throw new DomainException($"Unsupported payment method '{paymentMethod}'.");
        }
        paymentMethodService.Pay(finalAmount);
        // 4. save
        _orderLog.Record(order.Id.ToString(), finalAmount);

        // 5. notify
        _notifier.NotifyOrderProcessed(email, order.Id.ToString(), finalAmount);
        return finalAmount;
    }

}