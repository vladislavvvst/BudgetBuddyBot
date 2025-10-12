using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Cache;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.Features.UI;

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

        // Отправить RPC сервису статистики ...
        // try
        // {
        //     
        // }
        // catch (RequestFaultException ex)
        // {
        //     context.Logger.LogError(ex, "Get statistic fault");
        //     await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorProcessing, cancellationToken: ct);
        // }
        // catch (RequestTimeoutException)
        // {
        //     context.Logger.LogError("AddCategory timeout");
        //     await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorTimeout, cancellationToken: ct);
        // }
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);
    }
}
