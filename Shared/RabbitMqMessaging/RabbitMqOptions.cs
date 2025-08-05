namespace RabbitMqMessaging;

public class RabbitMqOptions
{
    public const string RabbitMq = nameof(RabbitMq);
    public string HostName { get; set; } = default!;
    public string AddExpenseQueueName { get; set; } = default!;
}
