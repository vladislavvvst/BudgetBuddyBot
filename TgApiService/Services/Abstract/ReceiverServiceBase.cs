using Telegram.Bot;
using Telegram.Bot.Polling;

namespace TgApiService.Services.Abstract;

internal abstract class ReceiverServiceBase<TUpdateHandler> : IReceiverService where TUpdateHandler : IUpdateHandler
{
    private readonly ILogger<ReceiverServiceBase<TUpdateHandler>> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly TUpdateHandler _updateHandler;

    public ReceiverServiceBase(ILogger<ReceiverServiceBase<TUpdateHandler>> logger, ITelegramBotClient botClient, TUpdateHandler updateHandler)
    {
        _logger = logger;
        _botClient = botClient;
        _updateHandler = updateHandler;
    }

    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        ReceiverOptions receiverOptions = new() { DropPendingUpdates = true, AllowedUpdates = [] };
        Telegram.Bot.Types.User me = await _botClient.GetMe(stoppingToken);

        _logger.LogInformation("Start receiving updates for {BotName}", me.Username ?? "My Awesome Bot");

        await _botClient.ReceiveAsync(_updateHandler, receiverOptions, stoppingToken);
    }
}
