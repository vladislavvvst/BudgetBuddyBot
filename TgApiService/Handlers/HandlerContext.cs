using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Services;

namespace TgApiService.Handlers;

internal readonly record struct HandlerContext
(
    ILogger Logger,
    IUserStateStorage StateStorage,
    ISpendingTrackerGateway Tracker,
    ITelegramBotClient Bot,
    Update Update
);
