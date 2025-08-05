using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqMessaging.Connection;
using System.Text.Json;

namespace RabbitMqMessaging.Subscriber;

internal class RabbitMqSubscriber<T> : IMessageSubscriber<T> where T : class
{
    private readonly IRabbitMqConnectionProvider _provider;
    private readonly List<string> _consumerTags = [];
    private IChannel? _channel;

    public RabbitMqSubscriber(IRabbitMqConnectionProvider provider)
    {
        _provider = provider;
    }

    public async Task SubscribeAsync(Func<T, Task> handler, string queueName)
    {
        if (_channel is null)
        {
            IConnection connection = await _provider.GetConnectionAsync();
            _channel = await connection.CreateChannelAsync();
        }

        await _channel.QueueDeclareAsync
        (
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += async (s, ea) =>
        {
            try
            {
                T? msg = JsonSerializer.Deserialize<T>(ea.Body.Span)
                    ?? throw new InvalidOperationException("Deserialized message is null");

                await handler(msg);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception)
            {
                /* Ошибка -> логирование или уведомление */
            }
        };

        string tag = await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer);
        _consumerTags.Add(tag);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is null)
            return;

        foreach (var tag in _consumerTags)
            await _channel.BasicCancelAsync(tag);

        await _channel.CloseAsync();
        await _channel.DisposeAsync();

        _consumerTags.Clear();
        _channel = null;
    }
}
