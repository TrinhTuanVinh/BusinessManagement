using BusinessManagement.Infrastructure.Messaging;
using Testcontainers.RabbitMq;

public sealed class RabbitFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management").Build();
    public RabbitMqOptions Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var uri = new Uri(_container.GetConnectionString());
        var credentials = uri.UserInfo.Split(':');
        Options = new RabbitMqOptions
        {
            Host = uri.Host,
            Port = uri.Port,
            UserName = credentials[0],
            Password = credentials[1],
            RetryDelayMs = 300,
            MaxAttempts = 3
        };
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}