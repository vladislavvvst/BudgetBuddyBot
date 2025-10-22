using MassTransit;
using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.UI;

namespace TgApiService.Presentation.Telegram.Features.Categories;

/// <summary>
/// Сцена ввода названия новой категории.
/// Попадаем сюда из CategoryMenu "➕ Добавить".
/// Ждем текст от пользователя (имя новой категории).
/// </summary>
internal sealed class CategoryAddNameScene : IScene
{
    public UserState State => UserState.CategoryAddName;

    private const int MaxCategoryNameLength = 64;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.Bot.SendMessage(chatId, UiStrings.Prompts.CategoryAdd, parseMode: ParseMode.Html,
            replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        Message msg = context.Update.Message!;

        if (msg.Type is not MessageType.Text)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.TextExpected, cancellationToken: ct);
            return;
        }

        if (msg.Text is { Length: > MaxCategoryNameLength })
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.StringTooLong, cancellationToken: ct);
            return;
        }

        string name = NormalizeName(msg.Text ?? string.Empty);

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
            await context.Bot.SendMessage(chatId, msgDup, parseMode: ParseMode.Html, cancellationToken: ct);
            return;
        }

        try
        {
            string requestId = $"{chatId}:{msg.MessageId}";
            AddCategoryRequest request = new(chatId, name, requestId);
            AddCategoryResponse response = await context.Tracker.AddCategoryAsync(request, ct);

            if (response.Success)
                await context.Bot.SendMessage(chatId, UiStrings.CategoryAdded(name), parseMode: ParseMode.Html, cancellationToken: ct);
            else
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

        await SceneRegistry.NavigateBackAsync(context, UserState.CategoryMenu, ct);
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

        await context.Bot.SendMessage(chatId, UiStrings.Info.PushButton, cancellationToken: ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct) =>
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);

    private static string NormalizeName(string input) => input.Trim();
}
