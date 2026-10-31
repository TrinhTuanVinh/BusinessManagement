using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddTransient<IDiscountPolicy, VipDiscount>();
services.AddTransient<IDiscountPolicy, EmployeeDiscount>();
services.AddTransient<IDiscountPolicy, StudentDiscount>();
services.AddTransient<IDiscountPolicy>(_ => new FixedVoucherDiscount(50m));
services.AddTransient<RefundProcessor>();

services.AddTransient<IPaymentMethod, CashMethod>();

services.AddSingleton<BankPayment>();
services.AddSingleton<IPaymentMethod>(sp => sp.GetRequiredService<BankPayment>());
services.AddSingleton<IRefundablePayment>(sp => sp.GetRequiredService<BankPayment>());

services.AddTransient<INotifier, Notify>();
services.AddTransient<IOrderLog, SaveLog>();
services.AddTransient<OrderProcessor>();


using var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<OrderProcessor>();
