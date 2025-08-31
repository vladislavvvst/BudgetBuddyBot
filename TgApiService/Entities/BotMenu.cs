namespace TgApiService.Entities;

internal enum BotMenuAction
{
    AddExpense,
    ShowStats,
    ShowCategories,
    ShowAllExpenses
}

internal static class BotMenuMap
{
    public static string GetActionKey(BotMenuAction action) => action.ToString();

    public static bool TryParseActionKey(string? key, out BotMenuAction action) =>
        Enum.TryParse(key, out action);
}
