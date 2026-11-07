using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.Application.Checkout.Ports;
using BusinessManagement.Infrastructure.PaymentMethod;
using BusinessManagement.Infrastructure.Notify;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusinessManagement.Infrastructure.OrderLog;

namespace BusinessManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var merchantAccount = configuration["Payments:Bank:MerchantAccount"];
        if (string.IsNullOrWhiteSpace(merchantAccount))
        {
            throw new InvalidOperationException("Missing configuration 'Payments:Bank:MerchantAccount");
        }

        services.AddSingleton<LegacyBankGateway>();
        services.AddSingleton<IPaymentMethod>(sp => new LoggingPaymentMethod(
            new LegacyBankPaymentAdapter(sp.GetRequiredService<LegacyBankGateway>(), merchantAccount),
            sp.GetRequiredService<ILogger<LoggingPaymentMethod>>()
        ));
        services.AddSingleton<IPaymentMethod, CashMethod>();

        services.AddSingleton<INotifier, ConsoleNotify>();
        services.AddSingleton<IOrderLog, SaveLog>();
            
        return services;
    }
}