
using BusinessManagement.Application.Checkout.Discount;
using BusinessManagement.Application.Checkout.Events;
using BusinessManagement.Application.Checkout.Ports;
using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.Domain.Entities;

namespace BusinessManagement.UnitTests.Support;

public static class TestHelpers
{
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