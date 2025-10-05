using Telegram.Bot;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.MainMenu;

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
        await context.StateCache.SetStateAsync(chatId, UserState.MainMenu);

        await context.Bot.SendMessage(chatId, UiStrings.Prompts.StartText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Prompts.ChooseAction,
            replyMarkup: UiKeyboards.BuildMainMenuInline,
            cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        string text = context.Update.Message?.Text ?? string.Empty;

        if (string.Equals(text, UiStrings.Commands.About, StringComparison.Ordinal))
        {
            await context.Bot.SendMessage(
                chatId,
                UiStrings.Prompts.AboutBot,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        if (string.Equals(text, UiStrings.Commands.Menu, StringComparison.Ordinal) ||
            string.Equals(text, UiStrings.Commands.Start, StringComparison.Ordinal))
        {
            await EnterAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Prompts.ChooseAction,
            replyMarkup: UiKeyboards.BuildMainMenuInline,
            cancellationToken: ct);
    }

    public async Task OnCallbackAsync(UpdateContext context, CancellationToken ct)
    {
        string data = context.Update.CallbackQuery?.Data ?? string.Empty;

        if (string.Equals(data, UiStrings.CallbackData.NavBack, StringComparison.Ordinal))
        {
            await OnBackAsync(context, ct);
            return;
        }

        long chatId = Utils.ChatId(context);
        await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
    }

    public Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        return EnterAsync(context, ct);
    }
}
