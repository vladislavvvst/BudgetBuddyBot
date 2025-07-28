using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace TgApiService.Services;

internal class BotHostedService : BackgroundService, IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BotHostedService(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory, ILogger<BotHostedService> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ReceiverOptions receiverOptions = new() { DropPendingUpdates = true, AllowedUpdates = [] };
                User me = await _botClient.GetMe(stoppingToken);

                _logger.LogInformation("Start receiving updates for {BotName}", me.Username ?? "My Awesome Bot");

                await _botClient.ReceiveAsync
                (
                    updateHandler: this,
                    receiverOptions: receiverOptions,
                    cancellationToken: stoppingToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Polling failed with exception: {Exception}", ex);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var h = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
        await h.HandleUpdateAsync(botClient, update, cancellationToken);
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var h = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
        await h.HandleErrorAsync(botClient, exception, source, cancellationToken);
    }
}
