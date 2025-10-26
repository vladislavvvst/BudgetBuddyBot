namespace SharedTypes.Contracts;

//
// ----- DTO -----
//

/// <summary>
/// Трата
/// </summary>
public sealed record Expense
(
    long CategoryId,
    decimal Amount,
    string? Comment,
    DateTimeOffset AddedAtUtc
);

//
// ----- RPC -----
//

/// <summary>
/// Запрос на добавление пользователем траты
/// </summary>
public sealed record AddExpenseRequest
(
    long UserId,
    long CategoryId,
    decimal Amount,
    string? Comment,
    string RequestId
);

/// <summary>
/// Ответ на запрос на добавление пользователем траты
/// </summary>
public sealed record AddExpenseResponse
(
    bool Success
);

/// <summary>
/// Запрос на получение трат пользователя
/// </summary>
public sealed record GetExpensesRequest
(
    long UserId,
    int Page = 1,
    int PageSize = 10
);

/// <summary>
/// Ответ на запрос на получение трат пользователя
/// </summary>
public sealed record GetExpensesResponse
(
    IReadOnlyList<Expense> Expenses
);

/// <summary>
/// Оповещение о том, что добавилась трата (для сервиса статистики)
/// </summary>
public sealed record ExpenseAddedNotification
(
    long UserId,
    Category Category,
    Expense Expense
);
