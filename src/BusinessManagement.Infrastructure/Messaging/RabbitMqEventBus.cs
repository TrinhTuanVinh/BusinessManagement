using System.Text.Json;
using BusinessManagement.Application.Messaging;
using BusinessManagement.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace BusinessManagement.Infrastructure.Messaging;

public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private bool _topologyDeclared;

    public RabbitMqEventBus(RabbitMqOptions options) => _options = options;

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString("N"),
            Type = typeof(T).Name
        };

        await channel.BasicPublishAsync(RabbitMqTopology.Exchange, routingKey, mandatory: true, props,
            JsonSerializer.SerializeToUtf8Bytes(message), ct);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true } && _topologyDeclared) return _connection;
        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is not { IsOpen: true })
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    VirtualHost = _options.VirtualHost,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    AutomaticRecoveryEnabled = true
                };
                _connection = await factory.CreateConnectionAsync(ct);
                _topologyDeclared = false;
            }
            if (!_topologyDeclared)
            {
                await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);
                await RabbitMqTopology.DeclareAsync(channel, ct);
                _topologyDeclared = true;
            }
            return _connection;
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.CloseAsync();
        _gate.Dispose();
    }
}