namespace TgApiService.Entities;

internal enum BotMenuAction
{
    AddExpense,
    ShowStats,
    ShowCategories,
    ShowAllExpenses,
    Settings
}

internal static class BotMenuMap
{
    private static class BotMenu
    {
        public const string AddExpense = "➕ Добавить трату";
        public const string Stats = "📊 Статистика";
        public const string Categories = "🗂️ Категории";
        public const string AllExpenses = "📝 Все траты";
        public const string Settings = "⚙️ Настройки";
    }

    private static readonly Dictionary<BotMenuAction, string> _actionToText = new()
    {
        { BotMenuAction.AddExpense, BotMenu.AddExpense },
        { BotMenuAction.ShowStats, BotMenu.Stats },
        { BotMenuAction.ShowCategories, BotMenu.Categories },
        { BotMenuAction.ShowAllExpenses, BotMenu.AllExpenses },
        { BotMenuAction.Settings, BotMenu.Settings }
    };

    private static readonly Dictionary<string, BotMenuAction> _textToAction =
        _actionToText.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string GetText(BotMenuAction action) => _actionToText[action];

    public static bool TryGetAction(string? text, out BotMenuAction action)
    {
        action = default;
        return text != null && _textToAction.TryGetValue(text, out action);
    }

    public static string GetActionKey(BotMenuAction action) => action.ToString();

    public static bool TryParseActionKey(string? key, out BotMenuAction action) =>
        Enum.TryParse(key, out action);
}
