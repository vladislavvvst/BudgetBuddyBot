using SharedTypes;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgApiService.UI;

/// <summary>
/// Клавиатуры для UI.
/// </summary>
internal static class UiKeyboards
{
    public static InlineKeyboardMarkup CategoryMenuKb { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Add, UiStrings.CallbackData.CatAdd)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Delete, UiStrings.CallbackData.CatDel)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Back, UiStrings.CallbackData.NavBack)]
    ]);

    public static InlineKeyboardMarkup BuildMainMenuInline { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.AddExpense, UiStrings.CallbackData.AddExpense)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Stats, UiStrings.CallbackData.Stats)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Categories, UiStrings.CallbackData.Categories)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.LastExpenses, UiStrings.CallbackData.LastExpenses)]
    ]);
    
    public static InlineKeyboardMarkup BuildStatsMenuInline { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData("🔥 Полная статистика за 7 дней", "1")],
        [InlineKeyboardButton.WithCallbackData("Сегодня", "2")],
        [InlineKeyboardButton.WithCallbackData("7 дней", "3")],
        [InlineKeyboardButton.WithCallbackData("Месяц", "4")],
        [InlineKeyboardButton.WithCallbackData("📆 Диапазон", "5")],
        [InlineKeyboardButton.WithCallbackData("⬅️ Назад", "6")]
    ]);

    public static InlineKeyboardMarkup BuildCategoriesPickKb(IReadOnlyList<CategoryDto> categories)
    {
        List<InlineKeyboardButton[]> rows = [];

        for (int i = 0; i < categories.Count; i += 2)
        {
            List<InlineKeyboardButton> row = new(2)
            {
                InlineKeyboardButton.WithCallbackData(categories[i].Name, $"{UiStrings.CallbackData.ExpPickPrefix}{categories[i].Id}")
            };

            if (i + 1 < categories.Count)
                row.Add(InlineKeyboardButton.WithCallbackData(categories[i + 1].Name, $"{UiStrings.CallbackData.ExpPickPrefix}{categories[i + 1].Id}"));

            rows.Add(row.ToArray());
        }

        rows.Add([InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Back, UiStrings.CallbackData.NavBack)]);
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup BuildCategoriesDeleteKb(List<CategoryDto> categories)
    {
        List<InlineKeyboardButton[]> rows = [];

        for (int i = 0; i < categories.Count; i += 2)
        {
            List<InlineKeyboardButton> row = new(2)
            {
                InlineKeyboardButton.WithCallbackData(
                    categories[i].Name,
                    $"{UiStrings.CallbackData.CatDelPickPrefix}{categories[i].Id}")
            };

            if (i + 1 < categories.Count)
            {
                row.Add(InlineKeyboardButton.WithCallbackData(
                    categories[i + 1].Name,
                    $"{UiStrings.CallbackData.CatDelPickPrefix}{categories[i + 1].Id}"));
            }

            rows.Add(row.ToArray());
        }
        rows.Add([InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Back, UiStrings.CallbackData.NavBack)]);
        return new InlineKeyboardMarkup(rows);
    }
}
