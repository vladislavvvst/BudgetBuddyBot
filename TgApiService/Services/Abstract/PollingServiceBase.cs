namespace TgApiService.Services.Abstract;

internal abstract class PollingServiceBase<TReceiverService> : BackgroundService where TReceiverService : IReceiverService
{
    private readonly ILogger<PollingServiceBase<TReceiverService>> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PollingServiceBase(ILogger<PollingServiceBase<TReceiverService>> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<TReceiverService>();

                await receiver.ReceiveAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Polling failed with exception: {Exception}", ex);
                // Cooldown if something goes wrong
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
