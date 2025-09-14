using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Cache;
using TgApiService.Entities;

namespace TgApiService.Handlers;

internal static class TopMenuHandlers
{
    /// <summary>
    /// Показывает меню добавления расхода - выбор категории
    /// </summary>
    public static async Task Expense_ShowCategoriesAsync(HandlerContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.StateCache.SetStateAsync(chatId, UserState.ExpenseAdd_PickCategory);

        IReadOnlyList<CategoryDto> categories = await Utils.GetUserCategories(context, ct);

        if (categories.Count == 0)
        {
            await context.Bot.SendMessage(chatId, BotTexts.Info.CategoryNotFoundForAddExp, cancellationToken: ct);
            await Category_MenuAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, BotTexts.Prompts.ChooseCategory,
            replyMarkup: BuildCategoriesPickKb(categories), cancellationToken: ct);
    }

    /// <summary>
    /// Показывает заглушку для статистики (в разработке)
    /// </summary>
    public static async Task Stats_PlaceholderAsync(HandlerContext context, CancellationToken ct) =>
        await context.Bot.SendMessage(Utils.ChatId(context), BotTexts.Prompts.StatsPlaceholder, cancellationToken: ct);

    /// <summary>
    /// Показывает список категорий и возможности редактирования (просмотр, добавление, удаление)
    /// </summary>
    public static async Task Category_MenuAsync(HandlerContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.StateCache.SetStateAsync(chatId, UserState.CategoryMenu);

        IReadOnlyList<CategoryDto> categories = await Utils.GetUserCategories(context, ct);
        string text = BotTexts.CategoriesList(categories.Select(x => x.Name));

        await context.Bot.SendMessage(chatId, text,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: CategoryMenuKb,
            cancellationToken: ct);
    }

    /// <summary>
    /// Показывает последние 10 расходов пользователя
    /// </summary>
    public static async Task Expenses_ListAsync(HandlerContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        GetExpensesResponse response = await context.Tracker.GetExpensesAsync(new(chatId, 1, 10), ct);

        if (response.Items.Count == 0)
        {
            await context.Bot.SendMessage(chatId, BotTexts.Info.NoExpenses, cancellationToken: ct);
            return;
        }

        IReadOnlyList<CategoryDto> categories = await Utils.GetUserCategories(context, ct);
        Dictionary<long, string> byId = categories.ToDictionary(x => x.Id, x => x.Name);

        IEnumerable<string> lines = response.Items.Select(i =>
        {
            string categoryName = byId.TryGetValue(i.CategoryId, out string? name)
                ? name
                : $"{BotTexts.Errors.ErrorNameCategory}{i.CategoryId}";
            return BotTexts.ExpenseLine(i.AddedAtUtc, categoryName, i.Amount, i.Comment);
        });

        string text = $"{BotTexts.Info.LastExpensesHeader}\n{string.Join("\n", lines)}";
        await context.Bot.SendMessage(chatId, text, cancellationToken: ct);
    }

    #region Utils

    private static InlineKeyboardMarkup BuildCategoriesPickKb(IReadOnlyList<CategoryDto> categories)
    {
        List<InlineKeyboardButton[]> rows = [];

        for (int i = 0; i < categories.Count; i += 2)
        {
            List<InlineKeyboardButton> row = new(2)
            {
                InlineKeyboardButton.WithCallbackData(categories[i].Name, $"{BotTexts.Keys.ExpPickPrefix}{categories[i].Id}")
            };

            if (i + 1 < categories.Count)
                row.Add(InlineKeyboardButton.WithCallbackData(categories[i + 1].Name, $"{BotTexts.Keys.ExpPickPrefix}{categories[i + 1].Id}"));

            rows.Add(row.ToArray());
        }

        rows.Add([InlineKeyboardButton.WithCallbackData("⬅️ Назад", BotTexts.Keys.NavBack)]);
        return new InlineKeyboardMarkup(rows);
    }

    private static InlineKeyboardMarkup CategoryMenuKb { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.Add, BotTexts.Keys.CatAdd)],
        [InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.Delete, BotTexts.Keys.CatDel)],
        [InlineKeyboardButton.WithCallbackData(BotTexts.Buttons.Back, BotTexts.Keys.NavBack)]
    ]);

    #endregion
}
