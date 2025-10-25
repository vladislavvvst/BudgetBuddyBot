namespace SharedTypes.Contracts;

//
// ----- DTO -----
//

/// <summary>
/// DTO Итоги (Summary)
/// </summary>
public sealed record SummaryDto
(
    decimal Total,                       // Всего расходов за неделю
    decimal AvgPerDay,                   // Средний расход в день (Total/7)
    LargestExpenseDto LargestExpense,    // Крупнейшая трата (с подписью)
    LargestCategoryDto LargestCategory,  // Крупнейшая категория (с долей)
    DayPeakDto HighestSpendingDay        // Самый затратный день
);

/// <summary>
/// DTO Крупнейшая трата
/// </summary>
public sealed record LargestExpenseDto
(
    decimal Amount,
    string? Comment,
    DateOnly Day
)
{
    public static LargestExpenseDto Empty = new(0m, string.Empty, default);
};

/// <summary>
/// DTO Крупнейшая категория
/// </summary>
public sealed record LargestCategoryDto
(
    string Name,
    decimal Amount,
    decimal SharePercent
);

/// <summary>
/// DTO Элемент топ-5 категорий
/// </summary>
public sealed record CategoryShareDto
(
    string Name,
    decimal Amount,
    decimal SharePercent
);

/// <summary>
/// DTO Динамика по дням
/// </summary>
public sealed record DayAmountDto
(
    DateOnly Day,
    decimal Amount
);

/// <summary>
/// DTO Самый затратный день
/// </summary>
public sealed record DayPeakDto
(
    DateOnly Day,
    decimal Amount
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
    SummaryDto Summary,
    IReadOnlyList<CategoryShareDto> CategoriesTop5,
    IReadOnlyList<DayAmountDto> Days
)
{
    public static readonly GetStatsFullWeekResponse Empty =
        new(new SummaryDto(0m, 0m, new LargestExpenseDto(0m, string.Empty, default),
                new LargestCategoryDto(string.Empty, 0m, 0m),
                new DayPeakDto(default, 0m)),
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
    LargestExpenseDto LargestExpense
)
{
    public static GetStatsAmountResponse Empty = new(0m, 0m, LargestExpenseDto.Empty);
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
    IReadOnlyList<CategoryDto> Categories
);

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
    IReadOnlyList<DayAmountDto> Days
);
