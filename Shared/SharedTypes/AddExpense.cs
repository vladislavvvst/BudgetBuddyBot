namespace SharedTypes;

public sealed record AddExpense(ExpenseCategory Category, decimal Amount, string? Comment);
