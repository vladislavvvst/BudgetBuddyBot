using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Options;
using TgApiService.Scenes.Common;

namespace TgApiService.Services;

/// <summary>
/// Основной класс-обработчик логики update.
/// Выполняет валидацию (допущенные пользователи, личный чат),
/// оборачивает данные в UpdateContext и передает их в SceneRouter.
/// </summary>
internal class UpdateProcessor
{
    private readonly ILogger<UpdateProcessor> _logger;
    private readonly IOptions<TelegramOptions> _tgOptions;
    private readonly IStateCache _stateStorage;
    private readonly ISpendingTrackerGateway _tracker;

    public UpdateProcessor
    (
        ILogger<UpdateProcessor> logger, IOptions<TelegramOptions> tgOptions,
        IStateCache stateStorage, ISpendingTrackerGateway tracker
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

        // if (!IsAllowedAndPrivate(update))
        // {
        //     long? chatId = TryGetChatId(update);
        //
        //     if (chatId is { } id)
        //         await botClient.SendMessage(id, "Пока не для всех :(", cancellationToken: ct);
        //
        //     return;
        // }

        UpdateContext context = new(_logger, _stateStorage, _tracker, botClient, update);
        await SceneRouter.RouteAsync(context, ct);
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
