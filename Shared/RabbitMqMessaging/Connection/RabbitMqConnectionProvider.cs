using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RabbitMqMessaging.Connection;

internal class RabbitMqConnectionProvider : IRabbitMqConnectionProvider, IAsyncDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> opts)
    {
        ConnectionFactory factory = new() { HostName = opts.Value.HostName };
        _connection = factory.CreateConnectionAsync().Result;
    }

    public IConnection GetConnection() => _connection;

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
    }
}
