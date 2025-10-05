using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Stats;

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

        await context.Bot.SendMessage(
            chatId,
            "Выберите период для статистики:", // todo: в const string
            replyMarkup: UiKeyboards.BuildStatsMenuInline,
            cancellationToken: ct);
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

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Info.PushButton,
            cancellationToken: ct);
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

        // todo: в разработке ...
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        BackStackService.Pop(chatId);
        await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);
    }
}
