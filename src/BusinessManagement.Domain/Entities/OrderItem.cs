using BusinessManagement.Domain.Common;

namespace BusinessManagement.Domain.Entities;

public class OrderItem : BaseEntity
{
    private OrderItem() { }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    internal OrderItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than 0");
        }
        if (unitPrice < 0)
        {
            throw new DomainException("UnitPrice must be greater than 0");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
    internal void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than 0");
        }
        Quantity += quantity;
    }
}