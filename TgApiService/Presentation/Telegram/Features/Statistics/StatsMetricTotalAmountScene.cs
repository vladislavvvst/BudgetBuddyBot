using System.Globalization;
using MassTransit;
using SharedTypes.Contracts;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.UI;

namespace TgApiService.Presentation.Telegram.Features.Statistics;

/// <summary>
/// Сцена отображения страницы статистики с метрикой "Общая сумма за период"
/// </summary>
internal sealed class StatsMetricTotalAmountScene : IScene
{
    public UserState State => UserState.StatsMetricTotalAmount;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        PeriodsOfTime periodsOfTime = await context.StateCache.GetStatsPeriod(chatId);

        try
        {
            GetStatsAmountRequest request = new(chatId, periodsOfTime);
            GetStatsAmountResponse response = await context.Tracker.GetStatsAmountAsync(request, ct);

            string text = FormatTotalAmountStats(response);
            await context.Bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "Get statistic total amount fault");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("Get statistic total amount timeout");
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

    private static string FormatTotalAmountStats(GetStatsAmountResponse response)
    {
        string largestNote = string.IsNullOrWhiteSpace(response.LargestExpense?.Comment)
            ? "без комментария"
            : response.LargestExpense!.Comment;

        string largest =
            response.LargestExpense is { Amount: > 0 } le
                ? $"🔥 Крупнейшая трата: {Rub(le.Amount)} — {largestNote}"
                : "🔥 Крупнейшая трата: —";

        return $"💰 Всего расходов: {Rub(response.Amount)}\n📈 Средний расход в день: {Rub(response.AvgPerDay)}\n{largest}";

        string Rub(decimal value) => value.ToString("N2", CultureInfo.CurrentCulture) + " ₽";
    }
}
