using Microsoft.Extensions.Options;
using RabbitMqMessaging;
using RabbitMqMessaging.Subscriber;
using SharedTypes;

namespace SpendingTrackerService.Services;

internal class SpendTrackerService : BackgroundService
{
    private readonly ILogger<SpendTrackerService> _logger;
    private readonly IMessageSubscriber<AddExpenseMessage> _subscriber;
    private readonly IOptions<RabbitMqOptions> _mqOptions;

    public SpendTrackerService
    (
        ILogger<SpendTrackerService> logger, IMessageSubscriber<AddExpenseMessage> subscriber,
        IOptions<RabbitMqOptions> mqOptions
    )
    {
        _logger = logger;
        _subscriber = subscriber;
        _mqOptions = mqOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SpendWorker starting, subscribing to 'spend' queue");

        await _subscriber.SubscribeAsync
        (
            handler: OnMessageReceivedAsync,
            queueName: _mqOptions.Value.AddExpenseQueueName,
            logError: ex => _logger.LogError(ex, "Error processing message in 'spend' queue")
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException) { /* ожидаемо вышли по остановке */ }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SpendWorker stopping, disposing subscriber");
        await base.StopAsync(cancellationToken);
    }

    private async Task OnMessageReceivedAsync(AddExpenseMessage message)
    {
        _logger.LogInformation("Received message: {Message}", message);

        // например, сохраняем в БД, считаем статистику и т.п.
        await Task.CompletedTask;
    }
}
