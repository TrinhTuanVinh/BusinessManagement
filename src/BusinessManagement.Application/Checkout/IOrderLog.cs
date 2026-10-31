public interface IOrderLog
{
    void Record(string orderID, decimal finalAmount);
}