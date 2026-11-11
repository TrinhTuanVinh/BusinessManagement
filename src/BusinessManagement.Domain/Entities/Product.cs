using BusinessManagement.Domain.Common;

namespace BusinessManagement.Domain.Entities;
// Entities/Product.cs
public class Product : BaseEntity
{
    private Product() { }
    public string Name { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    public Product(string name, string category, decimal price, int stock)
    {

        if (stock < 0) throw new DomainException("Stock cannot be negative.");

        SetDetails(name, category, price);
        Stock = stock;
    }

    private void SetDetails(string name, string category, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (string.IsNullOrWhiteSpace(category)) throw new DomainException("Category is required.");
        if (price < 0) throw new DomainException("Price cannot be negative.");

        Name = name.Trim();
        Category = category.Trim();
        Price = price;
    }

    public void UpdateDetails(string name, string category, decimal price)
    {
        SetDetails(name, category, price);
        Touch();
    }
    public void DecreaseStock(int quantity)
    {
        if (quantity < 1)
        {
            throw new DomainException("quantity must be greater than 0");
        }
        if (quantity > Stock)
            throw new DomainException("Insufficient stock.");
        Stock -= quantity;
        Touch();

    }
    public void IncreaseStock(int quantity)
    {
        if (quantity < 1)
        {
            throw new DomainException("quantity must be greater than 0");
        }
        Stock += quantity;
        Touch();
    }
    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0)
        {
            throw new DomainException("newPrice cannot be negative.");
        }
        Price = newPrice;
        Touch();
    }
}