namespace SharedTypes;

// ----- КАТЕГОРИИ -----

// DTO категории
public sealed record CategoryDto(long Id, string Name, bool IsSystem);

// Получение категорий
public sealed record GetCategoriesRequest(long UserId);
public sealed record GetCategoriesResponse(IReadOnlyList<CategoryDto> Items);

// Добавление пользовательской категории
public sealed record AddCategoryRequest(long UserId, string Name, string RequestId);
public sealed record AddCategoryResponse(bool Success);

// Удаление пользовательской категории
public sealed record DeleteCategoryRequest(long UserId, long CategoryId, string RequestId);
public sealed record DeleteCategoryResponse(bool Success);

// Оповещение о том, что категории пользователя изменились (добавлена/удалена категория)
public sealed record UserCategoriesChangedNotification(long UserId, IReadOnlyList<CategoryDto> Items);

// Оповещение о том, что добавилась трата (для сервиса статистики)
public sealed record ExpenseAddedNotification(long UserId, CategoryDto Category, ExpenseDto Expense);

// ----- ТРАТЫ -----

// DTO траты
public sealed record ExpenseDto(long CategoryId, decimal Amount, string? Comment, DateTimeOffset AddedAtUtc);

// Добавление траты
public sealed record AddExpenseRequest(long UserId, long CategoryId, decimal Amount, string? Comment, string RequestId);
public sealed record AddExpenseResponse(bool Success);

// Получение трат
public sealed record GetExpensesRequest(long UserId, int Page = 1, int PageSize = 10);
public sealed record GetExpensesResponse(IReadOnlyList<ExpenseDto> Items);

// ----- СТАТИСТИКА -----

// Запросы на получение статистики за период

// Полная статистика за неделю
public sealed record GetStatsFullWeekRequest(long UserId);

public sealed record GetStatsFullWeekResponse(SummaryDto Summary, IReadOnlyList<CategoryShareDto> CategoriesTop5, IReadOnlyList<DayAmountDto> Days)
{
    public static readonly GetStatsFullWeekResponse Empty =
        new(new SummaryDto(0m, 0m, new LargestExpenseDto(0m, string.Empty, default),
                new LargestCategoryDto(string.Empty, 0m, 0m),
                new DayPeakDto(default, 0m)),
            [],
            []);
}
// Итоги (Summary)
public sealed record SummaryDto
(
    decimal Total,                       // Всего расходов за неделю
    decimal AvgPerDay,                   // Средний расход в день (Total/7)
    LargestExpenseDto LargestExpense,    // Крупнейшая трата (с подписью)
    LargestCategoryDto LargestCategory,  // Крупнейшая категория (с долей)
    DayPeakDto HighestSpendingDay        // Самый затратный день
);
// Крупнейшая трата
// Label: если есть комментарий — он; иначе имя категории. День — календарный (DateOnly).
public sealed record LargestExpenseDto(decimal Amount, string Note, DateOnly Day);
// Крупнейшая категория (имя, сумма, процент доли от Total)
public sealed record LargestCategoryDto(string Name, decimal Amount, decimal SharePercent);
// Элемент топ-5 категорий
public sealed record CategoryShareDto(string Name, decimal Amount, decimal SharePercent);
// Динамика по дням (ровно 7 элементов, хронологически)
public sealed record DayAmountDto(DateOnly Day, decimal Amount);
// Самый затратный день
public sealed record DayPeakDto(DateOnly Day, decimal Amount);
