using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Services;

namespace TgApiService.Handlers;

internal readonly record struct HandlerContext
(
    ILogger Logger,
    IStateCache StateCache,
    ISpendingTrackerGateway Tracker,
    ITelegramBotClient Bot,
    Update Update
);
