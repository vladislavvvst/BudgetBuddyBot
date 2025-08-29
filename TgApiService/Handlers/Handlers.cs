using MassTransit;
using SharedTypes;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Entities;
using TgApiService.Services;

namespace TgApiService.Handlers;

internal static class Handlers
{
    // ----- Главная/общие -----
    public static async Task StartAsync(HandlerContext context, CancellationToken ct)
    {
        await context.Bot.SendMessage(ChatId(context), BotTexts.StartText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
        await MainMenuAsync(context, ct);
    }

    public static async Task AboutAsync(HandlerContext context, CancellationToken ct) =>
        await context.Bot.SendMessage(ChatId(context), BotTexts.AboutBot,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);

    public static async Task MainMenuAsync(HandlerContext context, CancellationToken ct)
    {
        await context.StateStorage.SetStateAsync(ChatId(context), UserState.MainMenu);
        await context.Bot.SendMessage(ChatId(context), BotTexts.ChooseAction,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: BuildMainMenuInline, cancellationToken: ct);
    }

    public static async Task StatsPlaceholderAsync(HandlerContext context, CancellationToken ct) =>
        await context.Bot.SendMessage(ChatId(context), BotTexts.StatsPlaceholder, cancellationToken: ct);

    public static async Task CancelAsync(HandlerContext context, CancellationToken ct)
    {
        await context.StateStorage.SetStateAsync(ChatId(context), UserState.MainMenu);
        await context.Bot.SendMessage(ChatId(context), BotTexts.Cancelled, cancellationToken: ct);
        await MainMenuAsync(context, ct);
    }

    // ----- Добавление траты -----
    public static async Task Expense_StartAsync(HandlerContext context, CancellationToken ct)
    {
        long chatId = ChatId(context);
        await context.StateStorage.SetStateAsync(chatId, UserState.ExpenseAdd_WaitExpense);
        await context.Bot.SendMessage(chatId, BotTexts.StartExpensePrompt,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: ct);
    }

    // ожидаем строку: "<категория> <сумма> [комментарий]"
    public static async Task Expense_WaitExpenseAsync(HandlerContext context, CancellationToken ct)
    {
        Message msg = context.Update.Message!;
        long chatId = msg.Chat.Id;

        // Отмена
        if (string.Equals(msg.Text?.Trim(), BotTexts.Cancel, StringComparison.OrdinalIgnoreCase))
        {
            await CancelAsync(context, ct);
            return;
        }

        string? input = msg.Text?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            await context.Bot.SendMessage(chatId, BotTexts.EmptyInput, cancellationToken: ct);
            return;
        }

        string[] parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await context.Bot.SendMessage(chatId, BotTexts.BadFormat,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            return;
        }

        // Категория
        IReadOnlyList<CategoryDto> categories = await GetUserCategoriesAsync(context, chatId, ct);
        CategoryDto? found = ResolveCategoryOrNull(categories, parts[0]);
        if (found is null)
        {
            await context.Bot.SendMessage(chatId,
                BotTexts.CategoriesList(categories.Select(x => x.Name)),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            return;
        }

        // Сумма
        if (!TryParseAmountFlexible(parts[1], out decimal amount) || amount <= 0)
        {
            await context.Bot.SendMessage(chatId, BotTexts.BadAmount,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            return;
        }

        string? comment = parts.Length > 2 ? parts[2] : null;

        try
        {
            string requestId = $"{chatId}:{msg.MessageId}";
            AddExpenseRequest request = new(chatId, found.Id, amount, comment, requestId);
            AddExpenseResponse response = await context.Tracker.AddExpenseAsync(request, ct);

            if (response.Success)
            {
                // Трата успешно добавлена
                await context.Bot.SendMessage(chatId, BotTexts.ExpenseAdded(amount, found.Name, comment),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            }
            else
            {
                // Не удалось добавить трату
                await context.Bot.SendMessage(chatId, BotTexts.ErrorAddingExpense,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);

                context.Logger.LogWarning("AddExpense business failure: {@Resp}", response);
            }

            await MainMenuAsync(context, ct);
            return;
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "AddExpense fault");
            await context.Bot.SendMessage(chatId, BotTexts.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("AddExpense timeout");
            await context.Bot.SendMessage(chatId, BotTexts.ErrorTimeout, cancellationToken: ct);
        }

        // На исключениях возвращаем в главное меню
        await MainMenuAsync(context, ct);
    }

    public static async Task Expenses_ListAsync(HandlerContext context, CancellationToken ct)
    {
        long chatId = ChatId(context);
        GetExpensesResponse response = await context.Tracker.GetExpensesAsync(new(chatId, 1, 10), ct);

        if (response.Items.Count == 0)
        {
            await context.Bot.SendMessage(chatId, BotTexts.NoExpenses, cancellationToken: ct);
            return;
        }

        IReadOnlyList<CategoryDto> categories = await GetUserCategoriesAsync(context, chatId, ct);
        Dictionary<long, string> byId = categories.ToDictionary(x => x.Id, x => x.Name);

        IEnumerable<string> lines = response.Items.Select(i =>
        {
            string categoryName = byId.TryGetValue(i.CategoryId, out string? name)
                ? name
                : $"Категория #{i.CategoryId}";
            return BotTexts.ExpenseLine(i.AddedAtUtc, categoryName, i.Amount, i.Comment);
        });

        string text = $"{BotTexts.LastExpensesHeader}\n{string.Join("\n", lines)}";
        await context.Bot.SendMessage(chatId, text, cancellationToken: ct);
    }

    // ----- Категории -----

    public static async Task Category_MenuAsync(HandlerContext context, CancellationToken ct)
    {
        long chatId = ChatId(context);
        await context.StateStorage.SetStateAsync(chatId, UserState.CategoryMenu);

        GetCategoriesResponse response = await context.Tracker.GetCategoriesAsync(new(chatId), ct);
        string text = BotTexts.CategoriesList(response.Items.Select(x => x.Name));

        await context.Bot.SendMessage(chatId, text,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: CategoryMenuKb,
            cancellationToken: ct);
    }

    // Обработка нажатий в меню категорий
    public static async Task Category_MenuCallbackAsync(HandlerContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (data == BotTexts.CatAdd)
        {
            await context.StateStorage.SetStateAsync(chatId, UserState.CategoryAdd_WaitName);
            await context.Bot.SendMessage(chatId, BotTexts.CategoryAdd,
                replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
            return;
        }

        if (data == BotTexts.CatDel)
        {
            await context.StateStorage.SetStateAsync(chatId, UserState.CategoryDelete_WaitChoice);
            await Category_ShowForDeleteAsync(context, chatId, ct);
            return;
        }

        if (data == BotTexts.NavBack)
        {
            await MainMenuAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.UnknownCmd, cancellationToken: ct);
    }

    public static async Task Category_AddNameAsync(HandlerContext context, CancellationToken ct)
    {
        Message msg = context.Update.Message!;
        long chatId = msg.Chat.Id;

        if (string.Equals(msg.Text?.Trim(), BotTexts.Cancel, StringComparison.OrdinalIgnoreCase))
        {
            await CancelAsync(context, ct);
            return;
        }

        string? name = msg.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await context.Bot.SendMessage(chatId, BotTexts.EmptyNameCategory, cancellationToken: ct);
            return;
        }

        try
        {
            string requestId = $"{chatId}:{msg.MessageId}";
            AddCategoryRequest request = new(chatId, name, requestId);
            AddCategoryResponse response =  await context.Tracker.AddCategoryAsync(request, ct);

            if (response.Success)
            {
                // Категория успешно добавлена
                await context.Bot.SendMessage(chatId, BotTexts.CategotyAdded(name),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            }
            else
            {
                // Не удалось добавить категорию
                await context.Bot.SendMessage(chatId, BotTexts.ErrorAddingCategory,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);

                context.Logger.LogWarning("AddCategory business failure: {@Resp}", response);
            }

            await Category_MenuAsync(context, ct);
            return;
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "AddCategory fault");
            await context.Bot.SendMessage(chatId, BotTexts.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("AddCategory timeout");
            await context.Bot.SendMessage(chatId, BotTexts.ErrorTimeout, cancellationToken: ct);
        }

        // На исключениях возвращаем в меню категорий
        await Category_MenuAsync(context, ct);
    }

    public static async Task Category_DeleteChoiceCallbackAsync(HandlerContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (data.StartsWith(BotTexts.CatDelPickPrefix, StringComparison.Ordinal))
        {
            string idStr = data[BotTexts.CatDelPickPrefix.Length..];
            if (!long.TryParse(idStr, out long categoryId))
            {
                await context.Bot.SendMessage(chatId, BotTexts.UnknownCmd, cancellationToken: ct);
                return;
            }

            try
            {
                string requestId = $"{chatId}:{cq.Id}";
                DeleteCategoryRequest request = new(chatId, categoryId, requestId);
                DeleteCategoryResponse response = await context.Tracker.DeleteCategoryAsync(request, ct);

                if (response.Success)
                {
                    // Категория успешно удалена
                    await context.Bot.SendMessage(chatId, BotTexts.CategoryDeleted, cancellationToken: ct);
                }
                else
                {
                    // Не удалось удалить категорию
                    await context.Bot.SendMessage(chatId, BotTexts.ErrorDeletingCategory, cancellationToken: ct);
                }

                await Category_MenuAsync(context, ct);
                return;
            }
            catch (RequestFaultException ex)
            {
                context.Logger.LogError(ex, "DeleteCategory fault");
                await context.Bot.SendMessage(chatId, BotTexts.ErrorProcessing, cancellationToken: ct);
            }
            catch (RequestTimeoutException)
            {
                context.Logger.LogError("DeleteCategory timeout");
                await context.Bot.SendMessage(chatId, BotTexts.ErrorTimeout, cancellationToken: ct);
            }

            await Category_MenuAsync(context, ct);
            return;
        }

        if (data == BotTexts.CatMenu || data == BotTexts.NavBack)
        {
            await Category_MenuAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.UnknownCmd, cancellationToken: ct);
    }

    private static async Task Category_ShowForDeleteAsync(HandlerContext context, long chatId, CancellationToken ct)
    {
        GetCategoriesResponse response = await context.Tracker.GetCategoriesAsync(new GetCategoriesRequest(chatId), ct);

        List<CategoryDto> categories = response.Items.Where(x => !x.IsSystem).ToList();

        if (categories.Count == 0)
        {
            await context.Bot.SendMessage(chatId, BotTexts.NoCategories, cancellationToken: ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.ChooseCategoryToDelete,
            replyMarkup: BuildCategoriesDeleteKb(categories), cancellationToken: ct);
    }

    // ----- Утилиты / клавиатуры -----

    private static long ChatId(HandlerContext context) =>
        context.Update.Message?.Chat.Id ?? context.Update.CallbackQuery!.Message!.Chat.Id;

    private static InlineKeyboardMarkup BuildMainMenuInline { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.AddExpense),
            BotMenuMap.GetActionKey(BotMenuAction.AddExpense))],

        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.ShowStats),
            BotMenuMap.GetActionKey(BotMenuAction.ShowStats))],

        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.ShowCategories),
            BotMenuMap.GetActionKey(BotMenuAction.ShowCategories))],

        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.ShowAllExpenses),
            BotMenuMap.GetActionKey(BotMenuAction.ShowAllExpenses))]
    ]);

    private static InlineKeyboardMarkup CategoryMenuKb { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData("➕ Добавить", BotTexts.CatAdd)],
        [InlineKeyboardButton.WithCallbackData("🗑 Удалить", BotTexts.CatDel)],
        [InlineKeyboardButton.WithCallbackData("⬅️ В меню", BotTexts.NavBack)]
    ]);

    private static InlineKeyboardMarkup BuildCategoriesDeleteKb(List<CategoryDto> categories)
    {
        List<InlineKeyboardButton[]> rows = [];

        for (int i = 0; i < categories.Count; i += 2)
        {
            List<InlineKeyboardButton> row = new(2)
        {
            InlineKeyboardButton.WithCallbackData(
                categories[i].Name,
                $"{BotTexts.CatDelPickPrefix}{categories[i].Id}")
        };

            if (i + 1 < categories.Count)
            {
                row.Add(InlineKeyboardButton.WithCallbackData(
                    categories[i + 1].Name,
                    $"{BotTexts.CatDelPickPrefix}{categories[i + 1].Id}"));
            }

            rows.Add([.. row]);
        }

        rows.Add([InlineKeyboardButton.WithCallbackData("⬅️ Назад", BotTexts.CatMenu)]);
        return new InlineKeyboardMarkup(rows);
    }

    private static async Task<IReadOnlyList<CategoryDto>> GetUserCategoriesAsync(HandlerContext context, long userId, CancellationToken ct)
    {
        GetCategoriesResponse response = await context.Tracker.GetCategoriesAsync(new GetCategoriesRequest(userId), ct);
        return response.Items;
    }

    private static CategoryDto? ResolveCategoryOrNull(IReadOnlyList<CategoryDto> categories, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (long.TryParse(token, out long id))
            return categories.FirstOrDefault(x => x.Id == id);

        return categories.FirstOrDefault(x =>
            string.Equals(x.Name, token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseAmountFlexible(string input, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string s = input.Trim().Replace(" ", "").Replace("\u00A0", "");
        int dot = s.LastIndexOf('.'), comma = s.LastIndexOf(',');

        if (dot >= 0 && comma >= 0)
            s = comma > dot ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
        else if (comma >= 0)
            s = s.Replace(',', '.');

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}
