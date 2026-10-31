public class SaveLog : IOrderLog
{
    void IOrderLog.Record(string orderID, decimal finalAmount)
    {
        File.AppendAllText("orders.log", $"{orderID}:{finalAmount}\n");
    }
}