namespace BusinessManagement.UnitTests;

public class OrderTests
{
    [Fact]
    public void Confirm_ItemIsEmpty_ThrowDomainException()
    {
        var order = new Order(Guid.NewGuid());
        Assert.Throws<DomainException>(() => order.Confirm());
    }

    [Fact]
    public void AddItem_OrderIsConfirmed_ThrowDomainException()
    {
        var order = new Order(Guid.NewGuid());
        var product = new Product("Wood 1", "Wood", 19m, 10);
        order.AddItem(product, 1);
        order.Confirm();

        Assert.Throws<DomainException>(() => order.AddItem(product, 2));
    }


    [Fact]
    public void AddItem_AddItemWithNegativeQuantity_ThrowDomainException()
    {
        var order = new Order(Guid.NewGuid());
        var product = new Product("Wood 1", "Wood", 19m, 10);

        Assert.Throws<DomainException>(() => order.AddItem(product, 0));
    }


    [Fact]
    public void CalculateTotal_Sum2Product_TrueTotal()
    {
        var order = new Order(Guid.NewGuid());
        var product = new Product("Wood 1", "Wood", 19m, 10);
        var productB = new Product("Plastic 1", "Plastic", 10m, 10);
        order.AddItem(product, 1);
        order.AddItem(productB, 2);

        Assert.Equal(39M, order.Total);
    }

    [Fact]
    public void Cancel_WhenCancelTwice_ThrowDomainException()
    {
        var order = new Order(Guid.NewGuid());
        order.Cancel();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void AddItem_SameProductTwice_MergesIntoOneLine()
    {
        var product = new Product("Wood 1", "Wood", 19m, 10);
        var order = new Order(Guid.NewGuid());

        order.AddItem(product, 2);
        order.AddItem(product, 3);

        var item = Assert.Single(order.Items);   // đúng 1 dòng
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public void AddItem_UsesPriceAtTimeOfAdding_NotLaterPrice()
    {
        var product = new Product("Wood 1", "Wood", 19m, 10);
        var order = new Order(Guid.NewGuid());

        order.AddItem(product, 1);
        product.ChangePrice(50m);                 // giá đổi sau khi đã thêm vào đơn

        Assert.Equal(19m, order.Total);           // chính là câu hỏi 3
    }
}