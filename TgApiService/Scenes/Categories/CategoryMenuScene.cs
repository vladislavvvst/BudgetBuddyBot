using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Categories;

/// <summary>
/// Сцена управления категориями.
/// Показывает список категорий и клавиатуру с действиями: "➕ Добавить" / "🗑️ Удалить" / "⬅️ Назад".
/// </summary>
internal sealed class CategoryMenuScene : IScene
{
    public UserState State => UserState.CategoryMenu;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Push(chatId, UserState.MainMenu);
        await context.StateCache.SetStateAsync(chatId, UserState.CategoryMenu);

        IReadOnlyList<CategoryDto> categories = await Utils.GetUserCategories(context, ct);
        if (categories.Count == 0)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Info.NoCategories, cancellationToken: ct);
        }
        else
        {
            IEnumerable<string> lines = categories
                .OrderBy(c => c.IsSystem ? 1 : 0)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => c.IsSystem ? $"🔒 {UiStrings.HtmlText(c.Name)}" : UiStrings.HtmlText(c.Name));

            string msg = UiStrings.CategoriesList(lines);
            await context.Bot.SendMessage(
                chatId,
                msg,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: ct);
        }

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Prompts.ChooseAction,
            replyMarkup: UiKeyboards.CategoryMenuKb,
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
            UiStrings.Prompts.ChooseAction,
            replyMarkup: UiKeyboards.CategoryMenuKb,
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

        if (string.Equals(data, UiStrings.CallbackData.CatAdd, StringComparison.Ordinal))
        {
            await SceneRegistry.Resolve(UserState.CategoryAddWaitName).EnterAsync(context, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.CatDel, StringComparison.Ordinal))
        {
            await SceneRegistry.Resolve(UserState.CategoryDeleteWaitChoice).EnterAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        BackStackService.Pop(chatId);
        await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);
    }
}
