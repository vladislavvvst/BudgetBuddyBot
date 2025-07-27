namespace TgApiService.Entities;

internal enum ExpenseCategory
{
    Supermarkets,
    Fuel,
    Marketplaces,
    FastFood
}

internal static class ExpenseCategoryParser
{
    private static class ExpenseCategoryStrings
    {
        public const string Supermarkets = "Супермаркеты";
        public const string Fuel = "Топливо";
        public const string Marketplaces = "Маркетплейсы";
        public const string FastFood = "Фастфуд";
    }

    private static readonly Dictionary<string, ExpenseCategory> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        { ExpenseCategoryStrings.Supermarkets, ExpenseCategory.Supermarkets },
        { ExpenseCategoryStrings.Fuel, ExpenseCategory.Fuel },
        { ExpenseCategoryStrings.Marketplaces, ExpenseCategory.Marketplaces },
        { ExpenseCategoryStrings.FastFood, ExpenseCategory.FastFood }
    };

    private static readonly Dictionary<ExpenseCategory, string> _reverseMap = _map.ToDictionary(x => x.Value, x => x.Key);

    public static bool TryParse(string? input, out ExpenseCategory category)
    {
        category = default;
        return input != null && _map.TryGetValue(input.Trim(), out category);
    }

    public static IEnumerable<string> AllDisplayNames() => _map.Keys;

    public static string ToDisplayName(ExpenseCategory category) =>
        _reverseMap.TryGetValue(category, out var name) ? name : category.ToString();
}
