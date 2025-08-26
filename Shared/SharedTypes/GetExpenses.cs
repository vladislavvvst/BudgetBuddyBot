namespace SharedTypes;

public sealed record GetExpenses(int Page, int PageSize);

public sealed record ExpensesPage(int Total, int Page, int PageSize, IEnumerable<ExpenseDto> Items);

public sealed record ExpenseDto(string Category, decimal Amount, string? Comment, DateTimeOffset AddDateUtc);
