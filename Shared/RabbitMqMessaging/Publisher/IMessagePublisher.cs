namespace RabbitMqMessaging.Publisher;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string queueName);
}
