using SharedTypes.Contracts;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.UI;

namespace TgApiService.Presentation.Telegram.Features.Categories;

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
        IReadOnlyList<Category> categories = await Utils.GetUserCategories(context, ct);

        if (categories.Count is 0)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Info.NoCategories, parseMode: ParseMode.Html,
                replyMarkup: UiKeyboards.CategoryMenuKb, cancellationToken: ct);
        }
        else
        {
            IEnumerable<string> lines = categories
                .OrderBy(c => c.IsSystem ? 1 : 0)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => c.IsSystem ? $"⚙️ {UiStrings.HtmlText(c.Name)}" : $"👤 {UiStrings.HtmlText(c.Name)}");

            await context.Bot.SendMessage(chatId, UiStrings.CategoriesList(lines), parseMode: ParseMode.Html,
                replyMarkup: UiKeyboards.CategoryMenuKb, cancellationToken: ct);
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

        if (string.Equals(data, UiStrings.CallbackData.CatAdd, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.CategoryAddName, ct);
            return;
        }

        if (string.Equals(data, UiStrings.CallbackData.CatDel, StringComparison.Ordinal))
        {
            await SceneRegistry.NavigateForwardAsync(context, UserState.CategoryDelete, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct) =>
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);
}
