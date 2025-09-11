using MassTransit;
using SharedTypes;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Cache;
using TgApiService.Entities;

namespace TgApiService.Handlers;

internal static class UserStateHandlers
{
    public static async Task Expense_PickCategoryCallbackAsync(HandlerContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (data == BotTexts.Keys.NavBack)
        {
            await CommandsHandlers.MainMenuAsync(context, ct);
            return;
        }

        string prefix = BotTexts.Keys.ExpPickPrefix;
        if (!data.StartsWith(prefix, StringComparison.Ordinal))
        {
            await context.Bot.SendMessage(chatId, BotTexts.Errors.UnknownCmd, cancellationToken: ct);
            return;
        }

        string idStr = data[prefix.Length..];
        if (!long.TryParse(idStr, out long categoryId))
        {
            await context.Bot.SendMessage(chatId, BotTexts.Errors.UnknownCmd, cancellationToken: ct);
            return;
        }

        await context.StateCache.SetStateAsync(chatId, UserState.ExpenseAdd_WaitAmountComment);
        await context.StateCache.SetCategoryIdAsync(chatId, categoryId.ToString(CultureInfo.InvariantCulture));

        await context.Bot.SendMessage
        (
            chatId,
            BotTexts.Prompts.StartExpensePrompt,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(
                [[InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.Back, BotTexts.Keys.NavBack)]]),
            cancellationToken: ct
        );
    }

    public static async Task Expense_AmountBackCallbackAsync(HandlerContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (data == BotTexts.Keys.NavBack)
        {
            await context.StateCache.RemoveCategoryIdAsync(chatId);
            await context.StateCache.SetStateAsync(chatId, UserState.ExpenseAdd_PickCategory);
            await TopMenuHandlers.Expense_ShowCategoriesAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.Errors.UnknownCmd, cancellationToken: ct);
    }

    public static async Task Expense_WaitAmountCommentAsync(HandlerContext context, CancellationToken ct)
    {
        Message msg = context.Update.Message!;
        long chatId = msg.Chat.Id;
        string text = msg.Text?.Trim() ?? string.Empty;

        // (Опционально оставляем текстовый "Назад" как запасной путь)
        if (string.Equals(text, BotTexts.Keys.NavBack, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, BotTexts.Buttons.Back, StringComparison.OrdinalIgnoreCase))
        {
            await context.StateCache.RemoveCategoryIdAsync(chatId);
            await TopMenuHandlers.Expense_ShowCategoriesAsync(context, ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            await context.Bot.SendMessage(chatId, BotTexts.Errors.EmptyInput, cancellationToken: ct);
            return;
        }

        string? categoryIdStr = await context.StateCache.GetCategoryIdAsync(chatId);
        if (string.IsNullOrEmpty(categoryIdStr) || !long.TryParse(categoryIdStr, out long categoryId))
        {
            // потеряли контекст — начнём заново
            await TopMenuHandlers.Expense_ShowCategoriesAsync(context, ct);
            return;
        }

        string[] parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !TryParseAmountFlexible(parts[0], out decimal amount) || amount <= 0m)
        {
            await context.Bot.SendMessage(chatId, BotTexts.Errors.BadAmount,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            return;
        }

        string? comment = parts.Length > 1 ? parts[1] : null;

        try
        {
            string requestId = $"{chatId}:{msg.MessageId}";
            AddExpenseRequest request = new(chatId, categoryId, amount, comment, requestId);
            AddExpenseResponse response = await context.Tracker.AddExpenseAsync(request, ct);

            if (response.Success)
            {
                await context.Bot.SendMessage(chatId, BotTexts.ExpenseAdded(amount, $"#{categoryId}", comment),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            }
            else
            {
                await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorAddingExpense,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
                context.Logger.LogWarning("AddExpense business failure: {@Resp}", response);
            }
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "AddExpense fault");
            await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("AddExpense timeout");
            await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorTimeout, cancellationToken: ct);
        }
        finally
        {
            await context.StateCache.RemoveCategoryIdAsync(chatId);
        }

        await CommandsHandlers.MainMenuAsync(context, ct);
    }

    public static async Task Category_MenuCallbackAsync(HandlerContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (data == BotTexts.Keys.CatAdd)
        {
            await context.StateCache.SetStateAsync(chatId, UserState.CategoryAdd_WaitName);
            await context.Bot.SendMessage(chatId, BotTexts.Prompts.CategoryAdd,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            return;
        }

        if (data == BotTexts.Keys.CatDel)
        {
            await context.StateCache.SetStateAsync(chatId, UserState.CategoryDelete_WaitChoice);
            await Category_ShowForDeleteAsync(context, chatId, ct);
            return;
        }

        if (data == BotTexts.Keys.NavBack)
        {
            await CommandsHandlers.MainMenuAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.Errors.UnknownCmd, cancellationToken: ct);
    }

    public static async Task Category_DeleteChoiceCallbackAsync(HandlerContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (data.StartsWith(BotTexts.Keys.CatDelPickPrefix, StringComparison.Ordinal))
        {
            string idStr = data[BotTexts.Keys.CatDelPickPrefix.Length..];
            if (!long.TryParse(idStr, out long categoryId))
            {
                await context.Bot.SendMessage(chatId, BotTexts.Errors.UnknownCmd, cancellationToken: ct);
                return;
            }

            try
            {
                string requestId = $"{chatId}:{cq.Id}";
                DeleteCategoryRequest request = new DeleteCategoryRequest(chatId, categoryId, requestId);
                DeleteCategoryResponse response = await context.Tracker.DeleteCategoryAsync(request, ct);

                if (response.Success)
                {
                    await context.Bot.SendMessage(chatId, BotTexts.Info.CategoryDeleted, cancellationToken: ct);
                }
                else
                {
                    await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorDeletingCategory, cancellationToken: ct);
                }

                await TopMenuHandlers.Category_MenuAsync(context, ct);
                return;
            }
            catch (RequestFaultException ex)
            {
                context.Logger.LogError(ex, "DeleteCategory fault");
                await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorProcessing, cancellationToken: ct);
            }
            catch (RequestTimeoutException)
            {
                context.Logger.LogError("DeleteCategory timeout");
                await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorTimeout, cancellationToken: ct);
            }

            await TopMenuHandlers.Category_MenuAsync(context, ct);
            return;
        }

        if (data == BotTexts.Keys.CatMenu || data == BotTexts.Keys.NavBack)
        {
            await TopMenuHandlers.Category_MenuAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.Errors.UnknownCmd, cancellationToken: ct);
    }

    public static async Task Category_AddNameAsync(HandlerContext context, CancellationToken ct)
    {
        Message msg = context.Update.Message!;
        long chatId = msg.Chat.Id;

        if (string.Equals(msg.Text?.Trim(), BotTexts.Commands.Cancel, StringComparison.OrdinalIgnoreCase))
        {
            await CommandsHandlers.CancelAsync(context, ct);
            return;
        }

        string? name = msg.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await context.Bot.SendMessage(chatId, BotTexts.Errors.EmptyNameCategory, cancellationToken: ct);
            return;
        }

        try
        {
            string requestId = $"{chatId}:{msg.MessageId}";
            AddCategoryRequest request = new AddCategoryRequest(chatId, name, requestId);
            AddCategoryResponse response = await context.Tracker.AddCategoryAsync(request, ct);

            if (response.Success)
            {
                await context.Bot.SendMessage(chatId, BotTexts.CategoryAdded(name),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            }
            else
            {
                await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorAddingCategory,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);

                context.Logger.LogWarning("AddCategory business failure: {@Resp}", response);
            }

            await TopMenuHandlers.Category_MenuAsync(context, ct);
            return;
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "AddCategory fault");
            await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("AddCategory timeout");
            await context.Bot.SendMessage(chatId, BotTexts.Errors.ErrorTimeout, cancellationToken: ct);
        }

        await TopMenuHandlers.Category_MenuAsync(context, ct);
    }

    #region Utils

    private static InlineKeyboardMarkup BuildCategoriesDeleteKb(List<CategoryDto> categories)
    {
        List<InlineKeyboardButton[]> rows = new List<InlineKeyboardButton[]>();

        for (int i = 0; i < categories.Count; i += 2)
        {
            List<InlineKeyboardButton> row = new List<InlineKeyboardButton>(2)
            {
                InlineKeyboardButton.WithCallbackData(
                    categories[i].Name,
                    $"{BotTexts.Keys.CatDelPickPrefix}{categories[i].Id}")
            };

            if (i + 1 < categories.Count)
            {
                row.Add(InlineKeyboardButton.WithCallbackData(
                    categories[i + 1].Name,
                    $"{BotTexts.Keys.CatDelPickPrefix}{categories[i + 1].Id}"));
            }

            rows.Add(row.ToArray());
        }

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", BotTexts.Keys.CatMenu)
        });

        return new InlineKeyboardMarkup(rows);
    }

    private static async Task Category_ShowForDeleteAsync(HandlerContext context, long chatId, CancellationToken ct)
    {
        GetCategoriesResponse response = await context.Tracker.GetCategoriesAsync(new GetCategoriesRequest(chatId), ct);

        List<CategoryDto> categories = response.Items.Where(x => !x.IsSystem).ToList();

        if (categories.Count == 0)
        {
            await context.Bot.SendMessage(chatId, BotTexts.Info.NoCategories, cancellationToken: ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.Prompts.ChooseCategoryToDelete,
            replyMarkup: BuildCategoriesDeleteKb(categories), cancellationToken: ct);
    }

    private static bool TryParseAmountFlexible(string input, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string s = input.Trim().Replace(" ", "").Replace("\u00A0", "");
        int dot = s.LastIndexOf('.');
        int comma = s.LastIndexOf(',');

        if (dot >= 0 && comma >= 0)
            s = comma > dot ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
        else if (comma >= 0)
            s = s.Replace(',', '.');

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    #endregion
}
