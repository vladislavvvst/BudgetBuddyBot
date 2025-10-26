using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.UI;

namespace TgApiService.Presentation.Telegram.Features.Statistics;

/// <summary>
/// Сцена выбора метрики для статистики.
/// Попадаем сюда из StatsPeriodScene.
/// Показывает inline-клавиатуру с вариантами метрики.
/// </summary>
internal sealed class StatsMetricScene : IScene
{
    public UserState State => UserState.StatisticsMetric;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.Bot.SendMessage(chatId, UiStrings.Prompts.SelectMetricPrompt, parseMode: ParseMode.Html,
            replyMarkup: UiKeyboards.BuildStatsMetricInline, cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.Bot.SendMessage(chatId, UiStrings.Info.PushButton, cancellationToken: ct);
    }

    public async Task OnCallbackAsync(UpdateContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (string.Equals(data, UiStrings.CallbackData.NavBack, StringComparison.Ordinal))
        {
            await OnBackAsync(context, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.StatsMetricTotalAmount, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.StatsMetricTotalAmount, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.StatsMetricByCategory, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.StatsMetricByCategory, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.StatsMetricDynByDays, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.StatsMetricDynByDays, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct) =>
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);
}
