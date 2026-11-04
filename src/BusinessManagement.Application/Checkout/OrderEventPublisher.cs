using Microsoft.Extensions.Logging;

public class OrderEventPublisher
{
    private readonly IReadOnlyList<IOrderProcessedHandler> _handlers;
    private readonly ILogger<OrderEventPublisher> _logger;

    public OrderEventPublisher(IEnumerable<IOrderProcessedHandler> handlers,
                               ILogger<OrderEventPublisher> logger)
    {
        _handlers = handlers.ToList();
        _logger = logger;
    }

    public void Publish(OrderProcessed e)
    {
        foreach (var handler in _handlers)
        {
            try
            {
                handler.Handle(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed for order {OrderId}",
                    handler.GetType().Name, e.OrderId);
                // không ném lại: các handler còn lại vẫn phải chạy
            }
        }
    }
}