using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Application.Cache;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.Features.UI;

namespace TgApiService.Presentation.Telegram.Features.Stats;

/// <summary>
/// Сцена выбора периода для статистики.
/// Попадаем сюда из главного меню "📊 Статистика".
/// Показывает inline-клавиатуру с вариантами периода и быструю статистику за последнюю неделю.
/// </summary>
internal sealed class StatsPeriodScene : IScene
{
    public UserState State => UserState.StatisticsWaitPeriod;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Push(chatId, UserState.MainMenu);
        await context.StateCache.SetStateAsync(chatId, UserState.StatisticsWaitPeriod);

        await context.Bot.SendMessage(chatId, UiStrings.Prompts.ChoosePeriod,
            replyMarkup: UiKeyboards.BuildStatsMenuInline, cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        string text = context.Update.Message?.Text ?? string.Empty;

        if (string.Equals(text, UiStrings.Commands.Cancel, StringComparison.Ordinal))
        {
            await OnBackAsync(context, ct);
            return;
        }

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

        if (string.Equals(data, UiStrings.CallbackData.StatsFullWeek, StringComparison.Ordinal) ||
            string.Equals(data, UiStrings.CallbackData.StatsToday, StringComparison.Ordinal) ||
            string.Equals(data, UiStrings.CallbackData.Stats7Days, StringComparison.Ordinal) ||
            string.Equals(data, UiStrings.CallbackData.StatsMonth, StringComparison.Ordinal) ||
            string.Equals(data, UiStrings.CallbackData.StatsRange, StringComparison.Ordinal))
        {
            await context.Bot.SendMessage(chatId, "нету RPC к сервису статистики", cancellationToken: ct);
            return;
        }
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        BackStackService.Pop(chatId);
        await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);
    }
}
