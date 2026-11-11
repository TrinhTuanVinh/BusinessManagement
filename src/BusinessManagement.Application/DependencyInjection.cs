
using BusinessManagement.Application.Checkout.Discount;
using BusinessManagement.Application.Checkout.Events;
using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.Application.Checkout.Ports;
using BusinessManagement.Application.Checkout.Pricing;
using BusinessManagement.Application.Customers;
using BusinessManagement.Application.Products;
using BusinessManagement.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDiscountStrategy>(_ =>
            new PercentDiscount(1, 0.10m, c => IsCustomerType(c, "VIP")));
        services.AddSingleton<IDiscountStrategy>(_ =>
            new PercentDiscount(1, 0.20m, c => IsCustomerType(c, "Employee")));
        services.AddSingleton<IDiscountStrategy>(_ =>
            new PercentDiscount(1, 0.05m, c => IsCustomerType(c, "Student")));
        services.AddSingleton<IVoucherFactory, InMemoryVoucherFactory>();
        services.AddScoped<PricingService>();

        services.AddScoped<ProductService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<ReportService>();

        services.AddScoped<DiscountCalculator>();
        services.AddScoped<IOrderProcessedHandler, LogOrderHandler>();
        services.AddScoped<IOrderProcessedHandler, NotifyOrderHandler>();
        services.AddScoped<OrderEventPublisher>();
        services.AddScoped<OrderProcessor>();
        services.AddScoped<RefundProcessor>();

        return services;
    }
    private static bool IsCustomerType(DiscountContext c, string type)
    => string.Equals(c.CustomerType, type, StringComparison.OrdinalIgnoreCase);
}