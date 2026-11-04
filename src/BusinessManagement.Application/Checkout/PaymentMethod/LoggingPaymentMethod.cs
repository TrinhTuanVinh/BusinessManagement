using System.Diagnostics;
using Microsoft.Extensions.Logging;

public class LoggingPaymentMethod : IPaymentMethod
{
    private readonly IPaymentMethod _inner;
    private readonly ILogger<LoggingPaymentMethod> _logger;

    public LoggingPaymentMethod(IPaymentMethod inner, ILogger<LoggingPaymentMethod> logger)
        => (_inner, _logger) = (inner, logger);

    public string PaymentMethod => _inner.PaymentMethod;   // phải trả đúng khóa của inner

    public void Pay(decimal amount)
    {

        // TODO: LogInformation("Paying {Amount} via {Method}", ...) trước khi gọi
        _logger.LogInformation("Paying {Amount} via {Method}", amount, _inner.PaymentMethod);
        // đo bằng Stopwatch; gọi _inner.Pay(amount); log kèm {ElapsedMs}
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            _inner.Pay(amount);
            stopwatch.Stop();
            _logger.LogInformation("Paid {Amount} via {Method} in {ElapsedMs} ms",
          amount, _inner.PaymentMethod, stopwatch.ElapsedMilliseconds);
            // nếu exception: LogError(ex, ...) rồi `throw;`
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Payment of {Amount} via {Method} failed after {ElapsedMs} ms",
           amount, _inner.PaymentMethod, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}