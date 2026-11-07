namespace BusinessManagement.Application.Checkout.Ports;

public interface IOrderLog
{
    void Record(string orderID, decimal finalAmount);
}