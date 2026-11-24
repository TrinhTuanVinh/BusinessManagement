using BusinessManagement.Application.Orders;
using BusinessManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

[Collection("postgres")]
public sealed class OutboxEndToEndTests : IClassFixture<RabbitFixture>
{
    private readonly RabbitFixture _rabbit;
    private readonly PostgresFixture _postgres;

    public OutboxEndToEndTests(PostgresFixture postgres, RabbitFixture rabbit)
        => (_postgres, _rabbit) = (postgres, rabbit);


    [Fact]
    public async Task CreateOrder_FlowsThroughOutboxAndRabbitMq_ToExactlyOneNotification()
    {

        await using (var db = _postgres.CreateContext())
            await db.OutboxMessages.Where(m => m.ProcessedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ProcessedAt, DateTime.UtcNow));
        await CommonFunction.PrepareQueuesAsync(_rabbit).WithTimeout();
        var customer = new Customer("Buyer", $"{Guid.NewGuid():N}@test.com");
        var product = new Product($"P-{Guid.NewGuid():N}", "Cat", 100m, 10);
        await using (var db = _postgres.CreateContext())
        {
            db.Customers.Add(customer);
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        using var host = WorkerTestHost.Create(_postgres, _rabbit, outboxEnabled: true);
        await host.StartAsync().WithTimeout();
        try
        {
            OrderDto order;
            await using (var db = _postgres.CreateContext())
                order = await OrderServices.Create(db).CreateAsync(
                    customer.Id, new[] { new OrderLine(product.Id, 1) }, null, CancellationToken.None);

            await CommonFunction.WaitUntilAsync(async () =>
            {
                await using var db = _postgres.CreateContext();
                var notified = await db.Notifications.CountAsync(n => n.OrderId == order.Id) == 1;
                var sent = await db.OutboxMessages.AnyAsync(m =>
                    m.Payload.Contains(order.Id.ToString()) && m.ProcessedAt != null);
                return notified && sent;
            });
        }
        finally
        {
            await host.StopAsync().WithTimeout();                               
        }
    }
}