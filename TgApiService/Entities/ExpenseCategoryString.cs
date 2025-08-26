namespace TgApiService.Entities;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

internal static class ExpenseCategories
{
    private static readonly ImmutableArray<string> _items =
    [
        "Супермаркеты",
        "Топливо",
        "Маркетплейсы",
        "Фастфуд"
    ];

    private static readonly HashSet<string> _set = new(_items, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> All() => _items;

    public static bool IsValid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return _set.Contains(Norm(input));
    }

    public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string candidate = Norm(input);

        string? match = _items.FirstOrDefault(s => string.Equals(s, candidate, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return false;

        normalized = match;
        return true;
    }

    private static string Norm(string s) => string.Join(' ', s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
