namespace RabbitMqMessaging.Publisher;

public interface IMessagePublisher : IAsyncDisposable
{
    Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default);
}
