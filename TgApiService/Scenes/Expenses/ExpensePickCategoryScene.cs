using SharedTypes;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Expenses;

/// <summary>
/// Сцена выбора категории для новой траты.
/// Попадаем из MainMenu "➕ Добавить трату".
/// Показывает список категорий с inline-кнопками.
/// </summary>
internal sealed class ExpensePickCategoryScene : IScene
{
    public UserState State => UserState.ExpenseAddPickCategory;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Push(chatId, UserState.MainMenu);
        await context.StateCache.SetStateAsync(chatId, UserState.ExpenseAddPickCategory);

        IReadOnlyList<CategoryDto> categories = await Utils.GetUserCategories(context, ct);

        if (categories.Count is 0)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Info.CategoryNotFoundForAddExp,
                replyMarkup: UiKeyboards.CategoryMenuKb, cancellationToken: ct);
            return;
        }

        await context.Bot.SendMessage(chatId, UiStrings.Prompts.ChooseCategory,
            replyMarkup: UiKeyboards.BuildCategoriesPickKb(categories), cancellationToken: ct);
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

        if (!data.StartsWith(UiStrings.CallbackData.ExpPickPrefix, StringComparison.Ordinal))
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
            return;
        }

        string idStr = data.Substring(UiStrings.CallbackData.ExpPickPrefix.Length);
        if (!long.TryParse(idStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long categoryId))
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
            return;
        }

        await context.StateCache.SetCategoryIdAsync(chatId, categoryId.ToString(CultureInfo.InvariantCulture));
        await SceneRegistry.Resolve(UserState.ExpenseAddWaitAmountComment).EnterAsync(context, ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        BackStackService.Pop(chatId);
        await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);
    }
}
