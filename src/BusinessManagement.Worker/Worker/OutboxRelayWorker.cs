using BusinessManagement.Application.Messaging;

namespace BusinessManagement.Worker;

public sealed class OutboxRelayWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<OutboxRelayWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(config.GetValue("Outbox:PollIntervalMs", 1000));
        var batchSize = config.GetValue("Outbox:BatchSize", 20);

        while (!stoppingToken.IsCancellationRequested)
        {
            var published = 0;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                published = await scope.ServiceProvider.GetRequiredService<OutboxRelayService>()
                    .DispatchBatchAsync(batchSize, DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Outbox relay iteration failed"); }

            if (published < batchSize)                         // còn việc thì chạy tiếp ngay; hết việc thì chờ
            {
                try { await Task.Delay(interval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}