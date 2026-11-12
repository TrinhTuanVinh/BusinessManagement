using BusinessManagement.Application.Checkout.Payments;
using BusinessManagement.Application.Checkout.Ports;
using BusinessManagement.Infrastructure.PaymentMethod;
using BusinessManagement.Infrastructure.Notify;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusinessManagement.Infrastructure.OrderLog;
using BusinessManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Auth;

namespace BusinessManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var merchantAccount = configuration["Payments:Bank:MerchantAccount"];
        if (string.IsNullOrWhiteSpace(merchantAccount))
        {
            throw new InvalidOperationException("Missing configuration 'Payments:Bank:MerchantAccount'. " +
        "Set it with user-secrets or the environment variable Payments__Bank__MerchantAccount.");
        }
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                 "Missing connection string 'ConnectionStrings:Default'. " +
        "Set it with user-secrets or the environment variable ConnectionStrings__Default.");
        }
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());


        var jwt = new JwtOptions
        {
            Issuer = configuration["Jwt:Issuer"] ?? "",
            Audience = configuration["Jwt:Audience"] ?? "",
            Key = configuration["Jwt:Key"] ?? "",
            AccessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes) ? minutes : 30
        };
        jwt.Validate();
        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<BusinessManagement.Application.Auth.IPasswordHasher, IdentityPasswordHasher>();

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