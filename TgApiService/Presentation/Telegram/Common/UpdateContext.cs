using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Application.Abstractions;

namespace TgApiService.Presentation.Telegram.Common;

/// <summary>
/// Объединенный контекст, который сцены и роутер получают на каждый update.
/// Позволяет не таскать все зависимости по отдельности.
/// </summary>
internal sealed record UpdateContext
(
    ILogger Logger,
    IStateCache StateCache,
    ISpendingTrackerGateway Tracker,
    ITelegramBotClient Bot,
    Update Update
);
