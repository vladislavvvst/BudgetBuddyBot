using SharedTypes;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgApiService.Presentation.Telegram.Features.UI;

/// <summary>
/// Клавиатуры для UI.
/// </summary>
internal static class UiKeyboards
{
    public static InlineKeyboardMarkup BackOnlyKb { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Back, UiStrings.CallbackData.NavBack)]
    ]);

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
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.StatsFullWeek, UiStrings.CallbackData.StatsFullWeek)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.StatsToday,    UiStrings.CallbackData.StatsToday)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Stats7Days,    UiStrings.CallbackData.Stats7Days)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.StatsMonth,    UiStrings.CallbackData.StatsMonth)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.StatsRange,    UiStrings.CallbackData.StatsRange)],
        [InlineKeyboardButton.WithCallbackData(UiStrings.Buttons.Back,          UiStrings.CallbackData.NavBack)]
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
