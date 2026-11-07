using BusinessManagement.Application.Checkout.Events;

namespace BusinessManagement.Application.Checkout.Ports;

public class LogOrderHandler : IOrderProcessedHandler
{
    private readonly IOrderLog _log;
    public LogOrderHandler(IOrderLog log) => _log = log;

    public void Handle(OrderProcessed e) => _log.Record(e.OrderId.ToString(), e.FinalAmount);
}
