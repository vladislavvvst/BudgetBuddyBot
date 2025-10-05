using MassTransit;
using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Categories;

/// <summary>
/// Сцена ввода названия новой категории.
/// Попадаем сюда из CategoryMenu "➕ Добавить".
/// Ждем текст от пользователя (имя новой категории).
/// </summary>
internal sealed class CategoryAddNameScene : IScene
{
    public UserState State => UserState.CategoryAddWaitName;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Push(chatId, UserState.CategoryMenu);
        await context.StateCache.SetStateAsync(chatId, UserState.CategoryAddWaitName);

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Prompts.CategoryAdd,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        Message msg = context.Update.Message!;
        string input = msg.Text ?? string.Empty;

        if (string.Equals(input, UiStrings.Commands.Cancel, StringComparison.Ordinal))
        {
            await OnBackAsync(context, ct);
            return;
        }

        string name = NormalizeName(input);

        if (string.IsNullOrWhiteSpace(name))
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.EmptyNameCategory, cancellationToken: ct);
            return;
        }

        IReadOnlyList<CategoryDto> existing = await Utils.GetUserCategories(context, ct);
        bool duplicate = existing.Any(c => string.Equals(c.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            string msgDup = $"Категория {UiStrings.Bold(name)} уже существует";
            await context.Bot.SendMessage(chatId, msgDup, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            return;
        }

        try
        {
            string requestId = $"{chatId}:{msg.MessageId}";
            AddCategoryRequest request = new(chatId, name, requestId);
            AddCategoryResponse response = await context.Tracker.AddCategoryAsync(request, ct);

            if (response.Success)
            {
                await context.StateCache.SetCategoriesAsync(chatId, []);

                await context.Bot.SendMessage(
                    chatId,
                    UiStrings.CategoryAdded(name),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    cancellationToken: ct);

                BackStackService.Pop(chatId);
                await SceneRegistry.Resolve(UserState.CategoryMenu).EnterAsync(context, ct);
                return;
            }

            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorAddingCategory, cancellationToken: ct);
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "AddCategory fault");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("AddCategory timeout");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorTimeout, cancellationToken: ct);
        }
    }

    public async Task OnCallbackAsync(UpdateContext context, CancellationToken ct)
    {
        CallbackQuery? cq = context.Update.CallbackQuery;
        if (cq == null)
            return;

        string data = cq.Data ?? string.Empty;
        if (string.Equals(data, UiStrings.CallbackData.NavBack, StringComparison.Ordinal))
        {
            await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
            await OnBackAsync(context, ct);
            return;
        }

        long chatId = cq.Message!.Chat.Id;
        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
        await context.Bot.SendMessage(chatId, UiStrings.Info.PushButton, cancellationToken: ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        BackStackService.Pop(chatId);
        await SceneRegistry.Resolve(UserState.CategoryMenu).EnterAsync(context, ct);
    }

    private static string NormalizeName(string input)
    {
        return input.Trim();
    }
}
