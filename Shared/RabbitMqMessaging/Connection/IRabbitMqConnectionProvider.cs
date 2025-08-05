using RabbitMQ.Client;

namespace RabbitMqMessaging.Connection;

internal interface IRabbitMqConnectionProvider : IAsyncDisposable
{
    Task<IConnection> GetConnectionAsync();
}
