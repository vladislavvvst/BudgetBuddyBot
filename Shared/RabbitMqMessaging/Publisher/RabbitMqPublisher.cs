using RabbitMQ.Client;
using RabbitMqMessaging.Connection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace RabbitMqMessaging.Publisher;

internal class RabbitMqPublisher : IMessagePublisher
{
    private readonly IRabbitMqConnectionProvider _provider;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    public RabbitMqPublisher(IRabbitMqConnectionProvider provider)
    {
        _provider = provider;
    }

    public async Task PublishAsync<T>(T message, string queueName)
    {
        using IChannel channel = await _provider.GetConnection().CreateChannelAsync();

        await channel.QueueDeclareAsync
        (
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

        await channel.BasicPublishAsync
        (
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: new ReadOnlyMemory<byte>(body)
        );
    }
}
