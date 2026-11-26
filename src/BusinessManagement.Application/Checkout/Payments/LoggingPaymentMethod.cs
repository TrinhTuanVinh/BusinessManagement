namespace BusinessManagement.Application.Checkout.Payments;

using System.Diagnostics;
using Microsoft.Extensions.Logging;

public class LoggingPaymentMethod : IPaymentMethod
{
    private readonly IPaymentMethod _inner;
    private readonly ILogger<LoggingPaymentMethod> _logger;

    public LoggingPaymentMethod(IPaymentMethod inner, ILogger<LoggingPaymentMethod> logger)
        => (_inner, _logger) = (inner, logger);

    public string PaymentMethod => _inner.PaymentMethod;

    public void Pay(decimal amount)
    {

        _logger.LogInformation("Paying {Amount} via {Method}", amount, _inner.PaymentMethod);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            _inner.Pay(amount);
            stopwatch.Stop();
            _logger.LogInformation("Paid {Amount} via {Method} in {ElapsedMs} ms",
          amount, _inner.PaymentMethod, stopwatch.ElapsedMilliseconds);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Payment of {Amount} via {Method} failed after {ElapsedMs} ms",
           amount, _inner.PaymentMethod, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}