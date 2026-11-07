using BusinessManagement.Domain.Common;
using BusinessManagement.Domain.Entities;

namespace BusinessManagement.UnitTests.Entities;

public class OrderTests
{

    [Fact]
    public void ApplyVoucher_ValidCode_TrimsAndUppercases()
    {
        var order = new Order(Guid.NewGuid());

        order.ApplyVoucher("  sale50k ");

        Assert.Equal("SALE50K", order.VoucherCode);
        Assert.NotNull(order.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyVoucher_EmptyCode_ThrowsAndKeepsVoucherNull(string? code)
    {
        var order = new Order(Guid.NewGuid());

        var ex = Assert.Throws<DomainException>(() => order.ApplyVoucher(code!));

        Assert.Contains("Voucher code", ex.Message);
        Assert.Null(order.VoucherCode);
    }

    [Fact]
    public void ApplyVoucher_OrderAlreadyConfirmed_Throws()
    {
        var order = new Order(Guid.NewGuid());
        order.AddItem(new Product("Wood 1", "Wood", 19m, 10), 1);
        order.Confirm();

        var ex = Assert.Throws<DomainException>(() => order.ApplyVoucher("SALE50K"));

        Assert.Contains("pending", ex.Message);
        Assert.Null(order.VoucherCode);
    }
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