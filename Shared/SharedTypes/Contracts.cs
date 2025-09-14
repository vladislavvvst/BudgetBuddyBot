namespace SharedTypes;

// ----- КАТЕГОРИИ -----

// DTO категории
public sealed record CategoryDto(long Id, string Name, bool IsSystem);

// Получение категорий
public sealed record GetCategoriesRequest(long UserId);
public sealed record GetCategoriesResponse(long UserId, IReadOnlyList<CategoryDto> Items);

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
public sealed record GetExpensesResponse(long UserId, IReadOnlyList<ExpenseDto> Items, int Total);
