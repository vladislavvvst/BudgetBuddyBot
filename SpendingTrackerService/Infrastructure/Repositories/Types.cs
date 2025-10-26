using SharedTypes.Contracts;

namespace SpendingTrackerService.Infrastructure.Repositories;

internal sealed record AddCategoryResult(bool Success, bool Restored, long? CategoryId);
internal sealed record AddExpenseResult(bool Success, bool IsIdempotent, long? ExpenseId = null, DateTimeOffset? AddedAtUtc = null);
internal sealed record PagedExpenses(IReadOnlyList<Expense> Items, int Total, int Page, int PageSize);
