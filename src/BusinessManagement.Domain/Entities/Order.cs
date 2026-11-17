using BusinessManagement.Domain.Common;

namespace BusinessManagement.Domain.Entities;

public class Order : BaseEntity
{
    private Order() { }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Customer? Customer { get; private set; }
    public string? VoucherCode { get; private set; }
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal DiscountAmount { get; private set; }
    public decimal Subtotal => _items.Sum(x => x.Quantity * x.UnitPrice);
    public decimal Total => Subtotal - DiscountAmount;        // thay thuộc tính Total cũ

    public Order(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer ID must not be empty");
        }
        CustomerId = customerId;
        Status = OrderStatus.Pending;
    }
    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Order must be pending!");
        }
        if (product is null)
        {
            throw new DomainException("Product is required!");
        }
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than 0!");
        }
        var item = _items.FirstOrDefault(x => x.ProductId == product.Id);
        if (item != null)
        {
            item.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(new OrderItem
            (
                product.Id,
                quantity,
                product.Price
            ));
        }
        Touch();
    }
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Order must be pending!");
        }
        if (_items.Count < 1)
        {
            throw new DomainException("Order must has at least 1 item");
        }
        Status = OrderStatus.Confirmed;
        Touch();
    }
    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("Order has been cancelled!");
        }
        Status = OrderStatus.Cancelled;
        Touch();
    }

    public void ApplyVoucher(string code)
    {
        if (Status != OrderStatus.Pending) throw new DomainException("Order must be pending!");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Voucher code is required.");
        VoucherCode = code.Trim().ToUpperInvariant();
        Touch();
    }
    public void ApplyDiscount(decimal amount)
    {
        if (Status != OrderStatus.Pending) throw new DomainException("Order must be pending!");
        if (amount < 0) throw new DomainException("Discount cannot be negative.");
        if (amount > Subtotal) throw new DomainException("Discount cannot exceed the order subtotal.");
        DiscountAmount = amount;
        Touch();
    }
}