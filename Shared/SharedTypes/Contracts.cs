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
public sealed record GetStatsFullWeekResponse
(
    // Итоги
    decimal Total,              // Всего расходов (сумма)
    decimal AvgPerDay,          // Средний расход в день (сумма)
    decimal LargestExpense,     // Крупнейшая трата (сумма)
    // По категориям (топ 5)
    IEnumerable<(string Name, decimal Amount)> CategoriesAmount,
    // По дням (хронологически)
    IEnumerable<(DateTimeOffset Date, decimal Amount)> DaysAmount
);
