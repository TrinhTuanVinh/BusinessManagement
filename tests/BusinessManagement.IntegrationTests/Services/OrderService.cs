using BusinessManagement.Application.Checkout.Discount;
using BusinessManagement.Application.Checkout.Pricing;
using BusinessManagement.Application.Orders;
using BusinessManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public static class OrderServices
{
    public static OrderService Create(AppDbContext db, ILogger<OrderService>? logger = null)
        => new(db,
               new PricingService(new DiscountCalculator(Array.Empty<IDiscountStrategy>()), new InMemoryVoucherFactory()),
               logger ?? NullLogger<OrderService>.Instance);
}