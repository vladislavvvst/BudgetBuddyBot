using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Services;

namespace TgApiService.Common;

/// <summary>
/// Объединенный контекст, который сцены и роутер получают на каждый update.
/// Позволяет не таскать все зависимости по отдельности.
/// </summary>
internal readonly record struct UpdateContext
(
    ILogger Logger,
    IStateCache StateCache,
    ISpendingTrackerGateway Tracker,
    ITelegramBotClient Bot,
    Update Update
);
