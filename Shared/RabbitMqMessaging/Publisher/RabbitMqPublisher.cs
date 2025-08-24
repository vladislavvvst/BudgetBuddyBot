using RabbitMQ.Client;
using RabbitMqMessaging.Connection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace RabbitMqMessaging.Publisher;

internal class RabbitMqPublisher : IMessagePublisher
{
    private readonly IRabbitMqConnectionProvider _provider;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    public RabbitMqPublisher(IRabbitMqConnectionProvider provider)
    {
        _provider = provider;
        _semaphore = new(1,1);
    }

    public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_channel is null)
            {
                IConnection connection = await _provider.GetConnectionAsync(cancellationToken);
                _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            }

            await _channel.QueueDeclareAsync
            (
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );

            byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

            await _channel.BasicPublishAsync
            (
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: new ReadOnlyMemory<byte>(body),
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is null)
            return;

        await _channel.CloseAsync();
        await _channel.DisposeAsync();

        _channel = null;
    }
}
