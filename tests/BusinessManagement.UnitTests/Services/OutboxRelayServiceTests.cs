using BusinessManagement.Application.Messaging;
using BusinessManagement.Domain.Entities;
using BusinessManagement.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

[Collection("postgres")]
public sealed class OutboxRelayServiceTests(PostgresFixture postgres)
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private async Task ResetPendingAsync()
    {
        await using var db = postgres.CreateContext();
        await db.OutboxMessages.Where(m => m.ProcessedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ProcessedAt, DateTime.UtcNow));
    }

    private async Task<List<Guid>> SeedPendingAsync(int count)
    {
        var ids = new List<Guid>();
        await using var db = postgres.CreateContext();
        for (var i = 0; i < count; i++)
        {
            var id = Guid.CreateVersion7();
            ids.Add(id);
            db.OutboxMessages.Add(new OutboxMessage(id, RoutingKeys.OrderCreated, nameof(OrderCreated),
                JsonSerializer.Serialize(new OrderCreated(id, Guid.NewGuid(), Guid.NewGuid(), 100m, DateTime.UtcNow))));
        }
        await db.SaveChangesAsync();
        return ids;
    }

    private async Task<List<OutboxMessage>> LoadAsync(IEnumerable<Guid> ids)
    {
        await using var db = postgres.CreateContext();
        return await db.OutboxMessages.AsNoTracking().Where(m => ids.Contains(m.Id)).ToListAsync();
    }

    private async Task<int> DispatchAsync(FakeEventBus bus, int batchSize, DateTime now)
    {
        await using var db = postgres.CreateContext();            // mỗi lần chạy relay một DbContext riêng
        return await new OutboxRelayService(db, bus, NullLogger<OutboxRelayService>.Instance)
            .DispatchBatchAsync(batchSize, now, Ct);
    }

    [Fact]
    public async Task Dispatch_ThreePending_PublishesAllAndMarksProcessed()
    {
        await ResetPendingAsync();
        var ids = await SeedPendingAsync(3);
        var bus = new FakeEventBus();

        var sent = await DispatchAsync(bus, 20, DateTime.UtcNow);

        Assert.Equal(3, sent);
        Assert.All(bus.Published, m => Assert.Equal(RoutingKeys.OrderCreated, m.RoutingKey));
        Assert.Equal(ids.OrderBy(x => x), bus.Published.Select(m => m.MessageId).OrderBy(x => x));
        Assert.All(await LoadAsync(ids), r => Assert.NotNull(r.ProcessedAt));
    }

    [Fact]
    public async Task Dispatch_SecondCall_PublishesNothingMore()
    {
        await ResetPendingAsync();
        await SeedPendingAsync(3);
        var bus = new FakeEventBus();
        await DispatchAsync(bus, 20, DateTime.UtcNow);

        var sent = await DispatchAsync(bus, 20, DateTime.UtcNow);

        Assert.Equal(0, sent);
        Assert.Equal(3, bus.Published.Count);                       // không có tin nào bị gửi lại
    }

    [Fact]
    public async Task Dispatch_BrokerDown_RecordsFailureAndKeepsMessagePending()
    {
        await ResetPendingAsync();
        var ids = await SeedPendingAsync(1);
        var bus = new FakeEventBus { ThrowOnPublish = new InvalidOperationException("broker down") };
        var now = DateTime.UtcNow;

        var sent = await DispatchAsync(bus, 20, now);

        Assert.Equal(0, sent);
        var row = Assert.Single(await LoadAsync(ids));
        Assert.Equal(1, row.Attempts);
        Assert.Contains("broker down", row.LastError);
        Assert.Null(row.ProcessedAt);
        Assert.True(row.NextAttemptAt > now);                       // lùi dần: chưa được chọn lại ngay
    }

    [Fact]
    public async Task Dispatch_AfterFailure_WaitsUntilDueThenSucceeds()
    {
        await ResetPendingAsync();
        var ids = await SeedPendingAsync(1);
        var bus = new FakeEventBus { ThrowOnPublish = new InvalidOperationException("broker down") };
        var now = DateTime.UtcNow;
        await DispatchAsync(bus, 20, now);                          // lỗi lần 1

        await DispatchAsync(bus, 20, now);                          // cùng thời điểm: chưa tới hạn nên không được chọn
        Assert.Equal(1, Assert.Single(await LoadAsync(ids)).Attempts);

        bus.ThrowOnPublish = null;                                  // broker hồi phục
        var sent = await DispatchAsync(bus, 20, now.AddSeconds(10));

        Assert.Equal(1, sent);
        Assert.NotNull(Assert.Single(await LoadAsync(ids)).ProcessedAt);
    }

    [Fact]
    public async Task Dispatch_FailureMidBatch_StopsBatchAndLeavesRestUntouched()
    {
        await ResetPendingAsync();
        var ids = await SeedPendingAsync(3);
        var bus = new FakeEventBus { ThrowFromAttempt = 2 };        // lần gửi thứ hai trở đi lỗi

        var sent = await DispatchAsync(bus, 20, DateTime.UtcNow);

        Assert.Equal(1, sent);
        var rows = await LoadAsync(ids);                            // không giả định dòng nào là "thứ hai"
        Assert.Equal(1, rows.Count(r => r.ProcessedAt != null));
        Assert.Equal(1, rows.Count(r => r.Attempts == 1));
        Assert.Equal(1, rows.Count(r => r.Attempts == 0 && r.ProcessedAt == null));
    }

    [Fact]
    public async Task Dispatch_TwoRelaysInParallel_NeverPublishTheSameMessageTwice()
    {
        await ResetPendingAsync();
        var ids = await SeedPendingAsync(20);
        var bus = new FakeEventBus { PublishDelay = TimeSpan.FromMilliseconds(50) };
        var now = DateTime.UtcNow;

        await Task.WhenAll(DispatchAsync(bus, 10, now), DispatchAsync(bus, 10, now));
        for (var i = 0; i < 5 && await DispatchAsync(bus, 10, now) > 0; i++) { }

        var messageIds = bus.Published.Select(m => m.MessageId).ToList();
        Assert.Equal(20, messageIds.Count);
        Assert.Equal(20, messageIds.Distinct().Count());         
        Assert.All(await LoadAsync(ids), r => Assert.NotNull(r.ProcessedAt));
    }
}