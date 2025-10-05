using MassTransit;
using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Categories;

/// <summary>
/// Сцена удаления категории.
/// Рендерит список всех пользовательских (не системных) категорий,
/// ждет клик на cat:del:{id}, вызывает RPC DeleteCategoryAsync и возвращает в CategoryMenu.
/// </summary>
internal sealed class CategoryDeleteScene : IScene
{
    public UserState State => UserState.CategoryDeleteWaitChoice;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Push(chatId, UserState.CategoryMenu);
        await context.StateCache.SetStateAsync(chatId, UserState.CategoryDeleteWaitChoice);

        List<CategoryDto> categories = (await Utils.GetUserCategories(context, ct)).Where(c => !c.IsSystem).ToList();

        if (categories.Count == 0)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Info.NoCategories, cancellationToken: ct);
            return;
        }

        await context.Bot.SendMessage(chatId, UiStrings.Prompts.ChooseCategoryToDelete,
            replyMarkup: UiKeyboards.BuildCategoriesDeleteKb(categories), cancellationToken: ct);
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

        const string prefix = UiStrings.CallbackData.CatDelPickPrefix;
        if (!data.StartsWith(prefix, StringComparison.Ordinal))
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
            return;
        }

        string idStr = data.Substring(prefix.Length);
        if (!long.TryParse(idStr, out long categoryId))
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.UnknownCmd, cancellationToken: ct);
            return;
        }

        // Перед удалением вытаскиваем из кэша имя категории по categoryId
        IReadOnlyList<CategoryDto> categories = await context.StateCache.GetCategoriesAsync(chatId);
        string categoryName = categories.FirstOrDefault(c => c.Id == categoryId)?.Name ?? $"#{categoryId}";

        try
        {
            string requestId = $"{chatId}:{cq.Id}";
            DeleteCategoryRequest request = new(chatId, categoryId, requestId);
            DeleteCategoryResponse response = await context.Tracker.DeleteCategoryAsync(request, ct);

            if (response.Success)
            {
                await context.StateCache.SetCategoriesAsync(chatId, []);
                await context.Bot.SendMessage(chatId, UiStrings.CategoryDeleted(categoryName), parseMode: ParseMode.Html, cancellationToken: ct);
            }
            else
            {
                await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorDeletingCategory, cancellationToken: ct);
            }
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "DeleteCategory fault");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("DeleteCategory timeout");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorTimeout, cancellationToken: ct);
        }

        await SceneRegistry.Resolve(UserState.CategoryMenu).EnterAsync(context, ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        BackStackService.Pop(chatId);
        await SceneRegistry.Resolve(UserState.CategoryMenu).EnterAsync(context, ct);
    }
}
