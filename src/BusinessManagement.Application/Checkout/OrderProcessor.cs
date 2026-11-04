
public class OrderProcessor
{
    private readonly DiscountCalculator _discountCalculator;
    private readonly IReadOnlyDictionary<string, IPaymentMethod> _payments;
    private readonly IVoucherFactory _vouchers;
    private readonly OrderEventPublisher _events;

    public OrderProcessor(DiscountCalculator discountCalculator, IEnumerable<IPaymentMethod> payments, IVoucherFactory vouchers, OrderEventPublisher events)
    {
        _discountCalculator = discountCalculator;
        _payments = payments.ToDictionary(p => p.PaymentMethod, StringComparer.OrdinalIgnoreCase);
        _vouchers = vouchers;
        _events = events;
    }

    public decimal Process(Order order, string customerType, string paymentMethod, string email)
    {
        // 1. validate
        if (order.Items.Count == 0) throw new DomainException("Order must contain at least one item.");

        IDiscountStrategy? voucher = null;
        if (!string.IsNullOrWhiteSpace(order.VoucherCode))
        {
            voucher = _vouchers.Create(order.VoucherCode)
                ?? throw new DomainException($"Invalid voucher code '{order.VoucherCode}'.");
        }

        // 2. discount
        decimal discount = _discountCalculator.CalculateDiscount(new DiscountContext
        (customerType, order.VoucherCode, order.Total), voucher);
        var finalAmount = order.Total - discount;

        // 3. payment
        if (!_payments.TryGetValue(paymentMethod, out var paymentMethodService))
        {
            throw new DomainException($"Unsupported payment method '{paymentMethod}'.");
        }
        paymentMethodService.Pay(finalAmount);
        // 4. save
        _events.Publish(new OrderProcessed(order.Id, finalAmount, email));
        return finalAmount;
    }

}