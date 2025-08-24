using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqMessaging.Connection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace RabbitMqMessaging.Subscriber;

internal class RabbitMqSubscriber<T> : IMessageSubscriber<T> where T : class
{
    private readonly IRabbitMqConnectionProvider _provider;
    private readonly List<string> _consumerTags = [];
    private IChannel? _channel;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    public RabbitMqSubscriber(IRabbitMqConnectionProvider provider)
    {
        _provider = provider;
    }

    public async Task SubscribeAsync
    (
        Func<T, Task> handler, string queueName,
        Action<Exception> logError, CancellationToken cancellationToken
    )
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

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
            cancellationToken: cancellationToken);

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += async (s, ea) =>
        {
            try
            {
                T? msg = JsonSerializer.Deserialize<T>(ea.Body.Span, _jsonOptions)
                    ?? throw new InvalidOperationException("Deserialized message is null");

                await handler(msg);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logError(ex);
            }
        };

        string tag = await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer,
            cancellationToken: cancellationToken);

        _consumerTags.Add(tag);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is null)
            return;

        foreach (string tag in _consumerTags)
            await _channel.BasicCancelAsync(tag);

        await _channel.CloseAsync();
        await _channel.DisposeAsync();

        _consumerTags.Clear();
        _channel = null;
    }
}
