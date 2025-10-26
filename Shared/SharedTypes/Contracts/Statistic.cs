namespace SharedTypes.Contracts;

//
// ----- DTO -----
//

/// <summary>
/// DTO Итоги (Summary)
/// </summary>
public sealed record Summary
(
    decimal Total,                                  // Всего расходов за неделю
    decimal AvgPerDay,                              // Средний расход в день (Total/7)
    LargestExpenseDay LargestExpenseDay,         // Крупнейшая трата (с подписью)
    TotalSpendByCategory TotalSpendByCategory,   // Крупнейшая категория (с долей)
    DailyAmount DailyAmount                      // Самый затратный день
);

/// <summary>
/// DTO Крупнейшая трата за день
/// </summary>
public sealed record LargestExpenseDay
(
    decimal Amount,     // Сумма траты
    string? Comment,    // Комментарий к трате
    DateOnly Day        // День
)
{
    public static readonly LargestExpenseDay Empty = new(0m, string.Empty, default);
};

/// <summary>
/// DTO Сумма трат по категории
/// </summary>
public sealed record TotalSpendByCategory
(
    string Name,    // Имя категории
    decimal Amount  // Сумма трат по категории
);

/// <summary>
/// DTO Сумма за день
/// </summary>
public sealed record DailyAmount
(
    DateOnly Day,   // День
    decimal Amount  // Сумма трат за день
);

/// <summary>
/// Периоды выборок
/// </summary>
public enum PeriodsOfTime
{
    None,
    Day,
    Week,
    Month,
    Custom
}

/// <summary>
/// Временной промежуток выборки
/// </summary>
public readonly record struct DateOnlyRange(DateOnly Start, DateOnly End)
{
    public static DateOnlyRange FromInclusive(DateOnly start, DateOnly end)
        => start <= end ? new DateOnlyRange(start, end) : new DateOnlyRange(end, start);
}

//
// ----- RPC -----
//

/// <summary>
/// Запрос на получение полной статистики за неделю
/// </summary>
public sealed record GetStatsFullWeekRequest
(
    long UserId
);

/// <summary>
/// Ответ на запрос на получение полной статистики за неделю
/// </summary>
public sealed record GetStatsFullWeekResponse
(
    Summary Summary,
    IReadOnlyList<TotalSpendByCategory> CategoriesTop5,
    IReadOnlyList<DailyAmount> Days
)
{
    public static GetStatsFullWeekResponse Empty { get; } =
        new(new Summary(0m, 0m, new LargestExpenseDay(0m, string.Empty, default),
                new TotalSpendByCategory(string.Empty, 0m),
                new DailyAmount(default, 0m)),
            [],
            []);
}

/// <summary>
/// Запрос на получение общей суммы за период
/// </summary>
public sealed record GetStatsAmountRequest
(
    long UserId,
    PeriodsOfTime Period,
    DateOnly? StartDay = null,
    DateOnly? EndDay = null
);

/// <summary>
/// Ответ на запрос на запрос на получение общей суммы за период
/// </summary>
public sealed record GetStatsAmountResponse
(
    decimal Amount,
    decimal AvgPerDay,
    LargestExpenseDay LargestExpenseDay
)
{
    public static GetStatsAmountResponse Empty { get; } = new(0m, 0m, LargestExpenseDay.Empty);
};

/// <summary>
/// Запрос на получение топа категорий за период
/// </summary>
public sealed record GetStatsTopCategoryRequest
(
    long UserId,
    PeriodsOfTime Period,
    DateOnly? StartDay = null,
    DateOnly? EndDay = null
);

/// <summary>
/// Ответ на запрос на получение топа категорий за период
/// </summary>
public sealed record GetStatsTopCategoryResponse
(
    IReadOnlyList<TotalSpendByCategory> Categories
)
{
    public static GetStatsTopCategoryResponse Empty { get; } = new([]);
};

/// <summary>
/// Запрос на получение динамики по дням за период
/// </summary>
public sealed record GetStatsDaysRequest
(
    long UserId,
    PeriodsOfTime Period,
    DateOnly? StartDay = null,
    DateOnly? EndDay = null
);

/// <summary>
/// Ответ на запрос на получение динамики по дням за период
/// </summary>
public sealed record GetStatsDaysResponse
(
    IReadOnlyList<DailyAmount> Days
);
