using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMqMessaging;
using RabbitMqMessaging.Subscriber;
using SharedTypes;
using SpendingTrackerService.Database;

namespace SpendingTrackerService.Services;

internal class SpendTrackerService : BackgroundService
{
    private readonly ILogger<SpendTrackerService> _logger;
    private readonly IMessageSubscriber<AddExpenseMessage> _subscriber;
    private readonly IOptions<RabbitMqOptions> _mqOptions;
    private readonly IServiceScopeFactory _scopeFactory;

    public SpendTrackerService
    (
        ILogger<SpendTrackerService> logger, IMessageSubscriber<AddExpenseMessage> subscriber,
        IOptions<RabbitMqOptions> mqOptions, IServiceScopeFactory scopeFactory
    )
    {
        _logger = logger;
        _subscriber = subscriber;
        _mqOptions = mqOptions;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SpendWorker starting, subscribing to 'spend' queue");

        await _subscriber.SubscribeAsync
        (
            handler: msg => OnMessageReceivedAsync(msg, stoppingToken),
            queueName: _mqOptions.Value.AddExpenseQueueName,
            logError: ex => _logger.LogError(ex, "Error processing message in 'spend' queue"),
            cancellationToken: stoppingToken
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

    private async Task OnMessageReceivedAsync(AddExpenseMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received message: {Message}", message);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ExpensesRepository>();

        try
        {
            var entity = message.ToEntity();
            await repo.AddExpenseAsync(entity, cancellationToken);

            _logger.LogInformation("Expense saved: {Id} {Amount} {Category}", entity.Id, entity.Amount, entity.Category);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "DB error while saving expense. Message: {@Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving expense. Message: {@Message}", message);
        }

        await Task.CompletedTask;
    }
}
