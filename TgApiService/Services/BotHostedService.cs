using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TgApiService.Entities;

namespace TgApiService.Services;

internal class BotHostedService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotHostedService> _logger;
    private readonly IUpdateHandler _updateHandler;

    public BotHostedService
    (
        ITelegramBotClient botClient, IUpdateHandler updateHandler,
        ILogger<BotHostedService> logger
    )
    {
        _logger = logger;
        _updateHandler = updateHandler;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");

        await EnsureCommandsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ReceiverOptions receiverOptions = new() { DropPendingUpdates = true, AllowedUpdates = [] };
                User me = await _botClient.GetMe(stoppingToken);

                await _botClient.ReceiveAsync
                (
                    updateHandler: _updateHandler,
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

    private async Task EnsureCommandsAsync(CancellationToken cancellationToken) =>
        await _botClient.SetMyCommands(BotCommands.ToTelegram(), cancellationToken: cancellationToken);
}
