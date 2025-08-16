namespace SharedTypes;

public sealed record AddExpenseMessage(ExpenseCategory Category, decimal Amount, string? Comment);
