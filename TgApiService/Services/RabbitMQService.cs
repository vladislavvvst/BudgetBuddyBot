using RabbitMQ.Client;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace TgApiService.Services;

internal class RabbitMQService<T> : IRabbitMQService<T> where T : class
{
    private readonly ILogger<RabbitMQService<T>> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    public RabbitMQService(ILogger<RabbitMQService<T>> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync(T message, MessageBus messageBus)
    {
        ConnectionFactory factory = new() { HostName = messageBus.HostName };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync
        (
            queue: messageBus.QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync
        (
            exchange: string.Empty,
            routingKey: messageBus.QueueName,
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: new ReadOnlyMemory<byte>(body)
        );

        _logger.LogInformation("Published to RabbitMQ queue {Queue}: {Payload}", messageBus.QueueName, json);
    }
}
