using BusinessManagement.Application.Checkout.Events;

namespace BusinessManagement.UnitTests.Support;

public class SpyHandler : IOrderProcessedHandler
{
    private readonly string _name;
    private readonly List<string> _calls;
    private readonly Exception? _throws;

    public SpyHandler(string name, List<string> calls, Exception? throws = null)
        => (_name, _calls, _throws) = (name, calls, throws);

    public void Handle(OrderProcessed e)
    {
        _calls.Add(_name);            // ghi lại trước khi ném
        if (_throws is not null) throw _throws;
    }
}