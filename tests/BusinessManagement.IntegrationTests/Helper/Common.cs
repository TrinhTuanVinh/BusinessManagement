using System.Runtime.CompilerServices;
using BusinessManagement.Infrastructure.Messaging;
using RabbitMQ.Client;

public static class CommonFunction
{
    public static async Task PrepareQueuesAsync(RabbitFixture rabbit)
    {
        var factory = new ConnectionFactory
        {
            HostName = rabbit.Options.Host,
            Port = rabbit.Options.Port,
            UserName = rabbit.Options.UserName,
            Password = rabbit.Options.Password,
            VirtualHost = rabbit.Options.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Đảm bảo topology đã tồn tại
        await RabbitMqTopology.DeclareAsync(
            channel, rabbit.Options.RetryDelayMs, CancellationToken.None);

        // Xóa message cũ trước mỗi test
        await channel.QueuePurgeAsync(
            RabbitMqTopology.OrderCreatedQueue);

        await channel.QueuePurgeAsync(
            RabbitMqTopology.OrderCreatedDlq);

        await channel.QueuePurgeAsync(
      RabbitMqTopology.OrderCreatedRetryQueue);
    }
    public static async Task WaitUntilAsync(
       Func<Task<bool>> condition,
       TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(100);
        }

        Assert.Fail("Condition was not satisfied before timeout.");
    }
}
  public static class TestTimeouts
    {
        public static readonly TimeSpan Default = TimeSpan.FromSeconds(60);

        public static async Task WithTimeout(this Task task, TimeSpan? timeout = null,
            [CallerArgumentExpression(nameof(task))] string? what = null)
        {
            try { await task.WaitAsync(timeout ?? Default); }
            catch (TimeoutException) { throw new TimeoutException($"Timed out waiting for: {what}"); }
        }

        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan? timeout = null,
            [CallerArgumentExpression(nameof(task))] string? what = null)
        {
            try { return await task.WaitAsync(timeout ?? Default); }
            catch (TimeoutException) { throw new TimeoutException($"Timed out waiting for: {what}"); }
        }
    }