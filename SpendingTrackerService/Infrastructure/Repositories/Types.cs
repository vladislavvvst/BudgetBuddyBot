using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Infrastructure.Repositories;

internal sealed record AddCategoryResult(bool Success, bool Restored, bool IsIdempotent, long? CategoryId);
internal sealed record DeleteCategoryResult(bool Success, bool IsIdempotent);
internal sealed record AddExpenseResult(bool Success, bool IsIdempotent, CategoryEntity? CategoryEntity = null, long? ExpenseId = null, DateTimeOffset? AddedAtUtc = null);
internal sealed record PagedExpenses(IReadOnlyList<ExpenseEntity> Items, int Total, int Page, int PageSize);
