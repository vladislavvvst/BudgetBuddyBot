using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqMessaging.Connection;
using System.Text.Json;

namespace RabbitMqMessaging.Subscriber;

internal class RabbitMqSubscriber<T> : IMessageSubscriber<T> where T : class
{
    private readonly IRabbitMqConnectionProvider _provider;
    private IChannel? _channel;
    private string? _consumerTag;

    public RabbitMqSubscriber(IRabbitMqConnectionProvider provider)
    {
        _provider = provider;
    }

    public async Task SubscribeAsync(Func<T, Task> handler, string queueName)
    {
        _channel = await _provider.GetConnection().CreateChannelAsync();
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
                T? msg = JsonSerializer.Deserialize<T>(ea.Body.Span);

                if (msg is null)
                    throw new InvalidOperationException("Deserialized message is null");

                await handler(msg);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception)
            {

            }
        };

        _consumerTag = await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is null)
            return;

        if (!string.IsNullOrEmpty(_consumerTag))
            await _channel.BasicCancelAsync(_consumerTag);

        await _channel.CloseAsync();
        await _channel.DisposeAsync();

        _channel = null;
        _consumerTag = null;
    }
}
