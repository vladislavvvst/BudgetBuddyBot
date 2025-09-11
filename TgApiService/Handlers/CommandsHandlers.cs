using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Cache;
using TgApiService.Entities;

namespace TgApiService.Handlers;

internal static class CommandsHandlers
{
    public static async Task MainMenuAsync(HandlerContext context, CancellationToken ct)
    {
        await context.StateCache.SetStateAsync(ChatId(context), UserState.MainMenu);
        await context.Bot.SendMessage(ChatId(context), BotTexts.Prompts.ChooseAction,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: BuildMainMenuInline, cancellationToken: ct);
    }

    public static async Task StartAsync(HandlerContext context, CancellationToken ct)
    {
        await context.Bot.SendMessage(ChatId(context), BotTexts.Prompts.StartText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
        await MainMenuAsync(context, ct);
    }

    public static async Task AboutAsync(HandlerContext context, CancellationToken ct) =>
        await context.Bot.SendMessage(ChatId(context), BotTexts.Prompts.AboutBot,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);

    public static async Task CancelAsync(HandlerContext context, CancellationToken ct)
    {
        await context.StateCache.SetStateAsync(ChatId(context), UserState.MainMenu);
        await context.Bot.SendMessage(ChatId(context), BotTexts.Errors.Cancelled, cancellationToken: ct);
        await MainMenuAsync(context, ct);
    }

    #region Utils

    private static InlineKeyboardMarkup BuildMainMenuInline { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.AddExpense, BotMenuMap.GetActionKey(BotMenuAction.AddExpense))],
        [InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.Stats, BotMenuMap.GetActionKey(BotMenuAction.ShowStats))],
        [InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.Categories, BotMenuMap.GetActionKey(BotMenuAction.ShowCategories))],
        [InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.LastExpenses, BotMenuMap.GetActionKey(BotMenuAction.ShowAllExpenses))]
    ]);

    private static long ChatId(HandlerContext context) =>
        context.Update.Message?.Chat.Id ?? context.Update.CallbackQuery!.Message!.Chat.Id;

    #endregion
}
