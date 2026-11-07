using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
// // services.AddTransient<IDiscountPolicy, VipDiscount>();
// // services.AddTransient<IDiscountPolicy, EmployeeDiscount>();
// // services.AddTransient<IDiscountPolicy, StudentDiscount>();
// services.AddTransient<RefundProcessor>();

// // services.AddTransient<IPaymentMethod, CashMethod>();

// // services.AddSingleton<BankPayment>();
// // services.AddSingleton<IPaymentMethod>(sp => sp.GetRequiredService<BankPayment>());
// // services.AddSingleton<IRefundablePayment>(sp => sp.GetRequiredService<BankPayment>());

// // services.AddTransient<INotifier, Notify>();
// // services.AddTransient<IOrderLog, SaveLog>();
// services.AddTransient<OrderProcessor>();

// services.AddSingleton<IVoucherFactory, InMemoryVoucherFactory>();   // không giữ trạng thái nên Singleton an toàn
// services.AddTransient<DiscountCalculator>();

// // services.AddSingleton<LegacyBankGateway>();
// // services.AddSingleton<IPaymentMethod>(sp => new LoggingPaymentMethod(
// //     new LegacyBankPaymentAdapter(sp.GetRequiredService<LegacyBankGateway>(), "0123456789"),
// //     sp.GetRequiredService<ILogger<LoggingPaymentMethod>>()));

// services.AddTransient<IOrderProcessedHandler, LogOrderHandler>();
// services.AddTransient<IOrderProcessedHandler, NotifyOrderHandler>();
// services.AddTransient<OrderEventPublisher>();
// services.AddTransient<OrderProcessor>();
using var provider = services.BuildServiceProvider();
// var processor = provider.GetRequiredService<OrderProcessor>();
