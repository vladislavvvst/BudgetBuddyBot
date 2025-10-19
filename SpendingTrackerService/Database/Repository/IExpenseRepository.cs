namespace SpendingTrackerService.Database.Repository;

internal interface IExpenseRepository
{
    Task<AddExpenseResult> AddAsync(long userId, long categoryId, decimal amount, string? comment, string requestId, CancellationToken cancellationToken);
    Task<PagedExpenses> GetPagedAsync(long userId, int page, int pageSize, CancellationToken cancellationToken);
}
