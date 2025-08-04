namespace TgApiService.Services;

internal sealed record MessageBus(string HostName, string QueueName);

internal interface IRabbitMQService<T>
{
    Task PublishAsync(T message, MessageBus messageBus);
}
