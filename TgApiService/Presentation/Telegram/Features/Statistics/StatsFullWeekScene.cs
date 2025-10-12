using MassTransit;
using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Cache;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.Features.UI;

namespace TgApiService.Presentation.Telegram.Features.Statistics;

internal class StatsFullWeekScene : IScene
{
    public UserState State => UserState.StatsFullWeek;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        try
        {
            GetStatsFullWeekRequest request = new(chatId);
            GetStatsFullWeekResponse response = await context.Tracker.GetStatsFullWeekAsync(request, ct);

            string text = FormatFullWeekStats(response);

            await context.Bot.SendMessage
            (
                chatId,
                text,
                parseMode: ParseMode.Html,
                replyMarkup: UiKeyboards.BackOnlyKb,
                cancellationToken: ct
            );
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "Get statistic full week fault");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("Get statistic full week timeout");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorTimeout, cancellationToken: ct);
        }
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

        await context.Bot.SendMessage(chatId, UiStrings.Info.PushButton, cancellationToken: ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);
    }

    private static string FormatFullWeekStats(GetStatsFullWeekResponse stats)
    {
        System.Globalization.CultureInfo ru = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        System.Text.StringBuilder sb = new();

        sb.AppendLine("📊 <b>Полная статистика за 7 дней</b>");
        sb.AppendLine();
        sb.AppendLine($"💰 Всего расходов: <b>{stats.Total.ToString("N0", ru)} ₽</b>");
        sb.AppendLine($"📉 Средний расход в день: <b>{stats.AvgPerDay.ToString("N0", ru)} ₽</b>");
        sb.AppendLine($"🔥 Крупнейшая трата: <b>{stats.LargestExpense.ToString("N0", ru)} ₽</b>");
        sb.AppendLine();

        // Категории (топ-5)
        sb.AppendLine("📂 <b>Топ категорий</b>");
        if (stats.CategoriesAmount?.Any() == true)
        {
            foreach ((string name, decimal amount) in stats.CategoriesAmount)
                sb.AppendLine($"• {EscapeHtml(name)} — <b>{amount.ToString("N0", ru)} ₽</b>");
        }
        else
        {
            sb.AppendLine("— нет данных");
        }

        sb.AppendLine();

        // По дням
        sb.AppendLine("📈 <b>Динамика по дням</b>");
        if (stats.DaysAmount?.Any() == true)
        {
            var ordered = stats.DaysAmount.OrderBy(d => d.Date).ToList();
            foreach ((DateTimeOffset date, decimal amount) in ordered)
            {
                sb.AppendLine($"• {date.ToLocalTime():dd.MM} — <b>{amount.ToString("N0", ru)} ₽</b>");
            }

            var max = ordered.MaxBy(d => d.Amount);
            if (max.Amount > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"🔥 Самый затратный день: <b>{max.Date.ToLocalTime():dd.MM}</b> — <b>{max.Amount.ToString("N0", ru)} ₽</b>");
            }
        }
        else
        {
            sb.AppendLine("— нет данных");
        }

        return sb.ToString();
    }

    private static string EscapeHtml(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        return s
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
