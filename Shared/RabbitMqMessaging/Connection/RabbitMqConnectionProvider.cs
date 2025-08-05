using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RabbitMqMessaging.Connection;

internal class RabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    private readonly IOptions<RabbitMqOptions> _mqOptions;
    private IConnection? _connection;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> mqOptions)
    {
        _mqOptions = mqOptions;
    }

    public async Task<IConnection> GetConnectionAsync()
    {
        if (_connection is null)
        {
            ConnectionFactory factory = new() { HostName = _mqOptions.Value.HostName };
            _connection = await factory.CreateConnectionAsync();
        }
        return _connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is null)
            return;

        await _connection.CloseAsync();
        await _connection.DisposeAsync();

        _connection = null;
    }
}
