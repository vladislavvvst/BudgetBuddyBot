using SharedTypes;

namespace SpendingTrackerService.Database.Repository;

internal sealed record AddCategoryResult(bool Success, bool Restored, long? CategoryId);
internal sealed record AddExpenseResult(bool Success, bool IsIdempotent, long? ExpenseId);
internal sealed record PagedExpenses(IReadOnlyList<ExpenseDto> Items, int Total, int Page, int PageSize);
