
using System.Net;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Application.Notifications;
using BusinessManagement.Infrastructure.Persistence;
using BusinessManagement.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

[Collection("postgres")]
public sealed class OrderCreatedHandlerTests
{
    private readonly PostgresFixture _postgres;

    public OrderCreatedHandlerTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    private static OrderCreated CreateEvent(
        Guid? eventId = null,
        Guid? orderId = null)
    {
        return new OrderCreated(
            eventId ?? Guid.NewGuid(),
            orderId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            150_000m,
            DateTime.UtcNow);
    }

    private OrderCreatedHandler CreateHandler(AppDbContext db)
    {
        return new OrderCreatedHandler(
            db,
            NullLogger<OrderCreatedHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_Sequential_CreatesOneNotification()
    {
        var message = CreateEvent();

        await using (var db = _postgres.CreateContext())
        {
            var handler = CreateHandler(db);

            await handler.HandleAsync(message, CancellationToken.None);
        }

        await using (var db = _postgres.CreateContext())
        {
            var handler = CreateHandler(db);

            await handler.HandleAsync(message, CancellationToken.None);
        }

        await using var verifyDb = _postgres.CreateContext();

        Assert.Equal(
            1,
            await verifyDb.Notifications.CountAsync(
                x => x.OrderId == message.OrderId));

        Assert.Equal(
            1,
            await verifyDb.ProcessedMessages.CountAsync(
                x => x.EventId == message.EventId &&
                     x.Consumer == OrderCreatedHandler.ConsumerName));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_Concurrent_CreatesOneNotification()
    {
        var message = CreateEvent();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await gate.Task;
            await using var db = _postgres.CreateContext();

            var handler = CreateHandler(db);

            await handler.HandleAsync(
                message,
                CancellationToken.None);
        }).ToArray();

        gate.SetResult();

        var exception = await Record.ExceptionAsync(
            () => Task.WhenAll(tasks));

        Assert.Null(exception);

        await using var verifyDb = _postgres.CreateContext();

        Assert.Equal(
            1,
            await verifyDb.Notifications.CountAsync(
                x => x.OrderId == message.OrderId));

        Assert.Equal(
            1,
            await verifyDb.ProcessedMessages.CountAsync(
                x => x.EventId == message.EventId &&
                     x.Consumer == OrderCreatedHandler.ConsumerName));
    }

    [Fact]
    public async Task HandleAsync_DifferentEventIds_SameOrder_CreatesTwoNotifications()
    {
        var orderId = Guid.NewGuid();

        var first = CreateEvent(orderId: orderId);
        var second = CreateEvent(orderId: orderId);

        await using (var db = _postgres.CreateContext())
        {
            var handler = CreateHandler(db);

            await handler.HandleAsync(first, CancellationToken.None);
            await handler.HandleAsync(second, CancellationToken.None);
        }

        await using var verifyDb = _postgres.CreateContext();

        Assert.Equal(
            2,
            await verifyDb.Notifications.CountAsync(
                x => x.OrderId == orderId));

        Assert.Equal(
            2,
            await verifyDb.ProcessedMessages.CountAsync(
                x => x.Consumer == OrderCreatedHandler.ConsumerName &&
                     (x.EventId == first.EventId ||
                      x.EventId == second.EventId)));
    }
    [Fact]
    public async Task Api_OnEmptyDatabase_AppliesMigrationsAtStartup()
    {
        var connectionString = await _postgres.CreateEmptyDatabaseAsync();

        await using var factory = new ApiFactory();
        var client = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Default", connectionString);
            b.UseSetting("Database:MigrateOnStartup", "true");
        }).CreateClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }
    [Fact]
    public async Task Api_OnEmptyDatabase_WithoutFlag_DoesNotMigrate()
    {
        var connectionString = await _postgres.CreateEmptyDatabaseAsync();

        await using var factory = new ApiFactory();
        using var client = factory.WithWebHostBuilder(b =>
            b.UseSetting("ConnectionStrings:Default", connectionString)).CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
        Assert.NotEmpty(await db.Database.GetPendingMigrationsAsync());  
    }
}
