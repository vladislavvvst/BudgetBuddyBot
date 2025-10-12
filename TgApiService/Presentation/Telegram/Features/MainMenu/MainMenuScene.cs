using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Application.Cache;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.Features.UI;

namespace TgApiService.Presentation.Telegram.Features.MainMenu;

/// <summary>
/// Главная сцена бота.
/// Рисует приветствие и главное меню (команды /menu, /start, /about).
/// </summary>
internal sealed class MainMenuScene : IScene
{
    public UserState State => UserState.MainMenu;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Clear(chatId);

        await context.Bot.SendMessage(chatId, UiStrings.Prompts.ChooseAction,
            replyMarkup: UiKeyboards.BuildMainMenuInline, cancellationToken: ct);
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

        if (string.Equals(data, UiStrings.CallbackData.AddExpense, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.ExpensePickCategory, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.Categories, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.CategoryMenu, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.Stats, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.StatisticsPeriod, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.LastExpenses, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.ExpenseShowLast, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
    }

    public Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        return EnterAsync(context, ct);
    }
}
