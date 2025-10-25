using SharedTypes.Contracts;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.UI;

namespace TgApiService.Presentation.Telegram.Features.Statistics;

/// <summary>
/// Сцена выбора периода для статистики.
/// Попадаем сюда из главного меню "📊 Статистика".
/// Показывает inline-клавиатуру с вариантами периода и быструю статистику за последнюю неделю.
/// </summary>
internal sealed class StatsPeriodScene : IScene
{
    public UserState State => UserState.StatisticsPeriod;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.Bot.SendMessage(chatId, UiStrings.Prompts.ChoosePeriod,
            replyMarkup: UiKeyboards.BuildStatsMenuInline, cancellationToken: ct);
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

        if (string.Equals(data, UiStrings.CallbackData.StatsFullWeek, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.StatsFullWeek, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.StatsRange, StringComparison.Ordinal))
        {
            // todo: важно проверить при установке Custom что промежуток задан корректно
            await context.StateCache.SetStatsPeriod(chatId, PeriodsOfTime.Custom);
            await context.Bot.SendMessage(chatId, "Календаря пока нету :(", cancellationToken: ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.StatsToday, StringComparison.Ordinal))
            await context.StateCache.SetStatsPeriod(chatId, PeriodsOfTime.Day);

        if (string.Equals(data, UiStrings.CallbackData.Stats7Days, StringComparison.Ordinal))
            await context.StateCache.SetStatsPeriod(chatId, PeriodsOfTime.Week);

        if (string.Equals(data, UiStrings.CallbackData.StatsMonth, StringComparison.Ordinal))
            await context.StateCache.SetStatsPeriod(chatId, PeriodsOfTime.Month);

        await SceneRegistry.NavigateForwardAsync(context, UserState.StatisticsMetric, ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct) =>
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);
}
