using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Entities;
using TgApiService.Handlers;
using TgApiService.Options;

namespace TgApiService.Services;

internal class UpdateProcessor
{
    private readonly ILogger<UpdateProcessor> _logger;
    private readonly IOptions<TelegramOptions> _tgOptions;
    private readonly IUserStateStorage _stateStorage;
    private readonly ISpendingTrackerGateway _tracker;

    public UpdateProcessor
    (
        ILogger<UpdateProcessor> logger, IOptions<TelegramOptions> tgOptions,
        IUserStateStorage stateStorage, ISpendingTrackerGateway tracker
    )
    {
        _logger = logger;
        _tgOptions = tgOptions;
        _stateStorage = stateStorage;
        _tracker = tracker;
    }

    public async Task HandleErrorAsync
    (
        ITelegramBotClient botClient, Exception exception,
        HandleErrorSource source, CancellationToken cancellationToken
    )
    {
        _logger.LogError(exception, "HandleError (source={Source})", source);

        if (exception is Telegram.Bot.Exceptions.RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!IsAllowedAndPrivate(update))
        {
            long? chatId = TryGetChatId(update);

            if (chatId is long id)
                await botClient.SendMessage(id, !IsPrivate(update)
                    ? BotTexts.AccessDeniedPrivate
                    : BotTexts.AccessDeniedOwner,
                cancellationToken: ct);
            return;
        }

        HandlerContext ctx = new(_logger, _stateStorage, _tracker, botClient, update);
        await BotRouter.RouteAsync(ctx, ct);
    }

    private bool IsAllowedAndPrivate(Update update)
    {
        long? id = TryGetChatId(update);

        if (id is null)
            return false;

        bool allowed = id == _tgOptions.Value.AdminId || id == _tgOptions.Value.TestUserId;
        return allowed && IsPrivate(update);
    }

    private static bool IsPrivate(Update update) =>
        (update.Message?.Chat.Type ?? update.CallbackQuery?.Message?.Chat.Type) == ChatType.Private;

    private static long? TryGetChatId(Update update) =>
        update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
}
