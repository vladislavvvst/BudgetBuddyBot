using MassTransit;
using System.Globalization;
using System.Net;
using System.Text;
using SharedTypes.Contracts;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.UI;

namespace TgApiService.Presentation.Telegram.Features.Statistics;

internal sealed class StatsFullWeekScene : IScene
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
            await context.Bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
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

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct) =>
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);

    private static string FormatFullWeekStats(GetStatsFullWeekResponse stats)
    {
        StringBuilder sb = new();

        DateOnly startDay = DateOnly.FromDateTime(DateTime.Now);
        DateOnly endDay = startDay.AddDays(-6);

        // Заголовок с диапазоном
        sb.AppendLine($"📊 <b>Неделя: {startDay:dd.MM}–{endDay:dd.MM}</b>");
        sb.AppendLine();

        // Итоги
        SummaryDto s = stats.Summary;
        sb.AppendLine($"💰 Всего: <b>{Rub(s.Total)}</b>");
        sb.AppendLine($"📉 В день: <b>{Rub(s.AvgPerDay)}</b>");

        // Крупнейшая трата
        if (s.LargestExpense.Amount > 0m)
        {
            string label = string.IsNullOrWhiteSpace(s.LargestExpense.Comment)
                ? string.Empty
                : $" — {EscapeHtml(s.LargestExpense.Comment)}";
            string dayStr = s.LargestExpense.Day == default ? string.Empty : $" ({s.LargestExpense.Day:dd.MM})";
            sb.AppendLine($"🔥 Крупнейшая трата: <b>{Rub(s.LargestExpense.Amount)}</b>{label}{dayStr}");
        }

        // Самый затратный день
        if (s.HighestSpendingDay.Amount > 0m && s.HighestSpendingDay.Day != default)
        {
            sb.AppendLine($"🔥 Самый затратный день: {s.HighestSpendingDay.Day:dd.MM} — <b>{Rub(s.HighestSpendingDay.Amount)}</b>");
        }

        sb.AppendLine();

        // Категории (топ)
        sb.AppendLine("📂 <b>Топ категорий</b>");
        if (stats.CategoriesTop5 is { Count: > 0 })
        {
            decimal shownTotal = 0m;
            foreach (CategoryShareDto item in stats.CategoriesTop5)
            {
                string name = EscapeHtml(item.Name);
                shownTotal += item.Amount;
                string percent = item.SharePercent > 0m ? $" ({Percent(item.SharePercent)})" : string.Empty;
                sb.AppendLine($"• {name} — <b>{Rub(item.Amount)}</b>{percent}");
            }

            // Крупнейшая категория (из summary, если есть)
            if (!string.IsNullOrWhiteSpace(s.LargestCategory.Name) && s.LargestCategory.Amount > 0m)
            {
                string extra = s.LargestCategory.SharePercent > 0m ? $" ({Percent(s.LargestCategory.SharePercent)})" : string.Empty;
                sb.AppendLine($"🔥 Крупнейшая категория: {EscapeHtml(s.LargestCategory.Name)} — {Rub(s.LargestCategory.Amount)}{extra}");
            }

            // Остаток (если не всё показали)
            decimal leftover = Math.Max(0m, s.Total - shownTotal);
            if (leftover > 0m)
            {
                sb.AppendLine($"• Прочее — <b>{Rub(leftover)}</b>");
            }
        }
        else
        {
            sb.AppendLine("— нет данных");
        }

        sb.AppendLine();

        // Динамика по дням (7 строк, хронологически)
        sb.AppendLine("📈 <b>Динамика по дням</b>");
        if (stats.Days is { Count: > 0 })
        {
            foreach (DayAmountDto d in stats.Days.OrderBy(d => d.Day))
            {
                sb.AppendLine($"• {d.Day:dd.MM} — <b>{Rub(d.Amount)}</b>");
            }
        }
        else
        {
            sb.AppendLine("— нет данных");
        }

        return sb.ToString();
    }

    private static string Rub(decimal value) => value.ToString("N2", CultureInfo.CurrentCulture) + " ₽";
    private static string Percent(decimal percent) => percent.ToString("0.#", CultureInfo.CurrentCulture) + "%";
    private static string EscapeHtml(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
