using System.Text;
using BusinessManagement.Application.Abstractions;
using BusinessManagement.Application.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessManagement.Infrastructure.Messaging;

public class OutboxRelayService(IAppDbContext db, IEventBus bus, ILogger<OutboxRelayService> logger)
{
    public async Task<int> DispatchBatchAsync(int batchSize, DateTime utcNow, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var rows = await db.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAt" IS NULL AND "NextAttemptAt" <= {utcNow}
                ORDER BY "CreatedAt", "Id"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        var published = 0;
        foreach (var row in rows)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                await bus.PublishRawAsync(row.RoutingKey, row.Type, row.Id, Encoding.UTF8.GetBytes(row.Payload), cts.Token);
                row.MarkProcessed(utcNow);
                published++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }     // đang tắt: lưu những gì đã gửi
            catch (Exception ex)
            {
                row.MarkFailed(ex.Message, utcNow);
                logger.LogWarning(ex, "Failed to publish outbox message {OutboxId}; attempt {Attempts}", row.Id, row.Attempts);
                break;                                                                          // broker có thể đang hỏng: dừng lô
            }
        }

        await db.SaveChangesAsync(CancellationToken.None);      // luôn lưu dấu của những tin đã gửi, kể cả khi đang tắt
        await tx.CommitAsync(CancellationToken.None);
        return published;
    }
}