namespace RabbitMqMessaging.Subscriber;

public interface IMessageSubscriber<T> : IAsyncDisposable
{
    Task SubscribeAsync(Func<T, Task> handler, string queueName, Action<Exception> logError);
}
