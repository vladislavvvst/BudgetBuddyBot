using RabbitMQ.Client;

namespace RabbitMqMessaging.Connection;

internal interface IRabbitMqConnectionProvider
{
    IConnection GetConnection();
}
