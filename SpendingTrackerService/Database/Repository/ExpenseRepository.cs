using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedTypes;
using SpendingTrackerService.Database.Entities;

namespace SpendingTrackerService.Database.Repository;

internal sealed class ExpenseRepository : IExpenseRepository
{
    private readonly ILogger<ExpenseRepository> _logger;
    private readonly ApplicationDbContext _dbContext;

    public ExpenseRepository(ILogger<ExpenseRepository> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task<bool> ExistsByRequestIdAsync(long userId, string requestId, CancellationToken cancellationToken)
    {
        bool exists = await _dbContext.Expenses
            .AsNoTracking()
            .AnyAsync(e => e.UserId == userId && e.RequestId == requestId, cancellationToken);

        return exists;
    }

    public async Task<AddExpenseResult> AddAsync(long userId, long categoryId, decimal amount, string? comment, string requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId) || amount <= 0m)
            return new AddExpenseResult(false, false, null);

        bool alreadyHandled = await _dbContext.Expenses
            .AsNoTracking()
            .AnyAsync(e => e.UserId == userId && e.RequestId == requestId, cancellationToken);

        if (alreadyHandled)
            return new AddExpenseResult(true, true, null);

        // Категория должна принадлежать пользователю и быть активной
        bool categoryOk = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.Id == categoryId && !c.IsDeleted, cancellationToken);

        if (!categoryOk)
            return new AddExpenseResult(false, false, null);

        // Округляем до 2 знаков
        decimal roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        ExpenseEntity entity = new()
        {
            UserId = userId,
            CategoryId = categoryId,
            Amount = roundedAmount,
            Comment = comment,
            RequestId = requestId
        };

        try
        {
            await _dbContext.Expenses.AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new AddExpenseResult(true, false, entity.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return new AddExpenseResult(true, true, null);
        }
    }

    public async Task<PagedExpenses> GetPagedAsync(long userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        int safePage = Math.Max(1, page);
        int safeSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<ExpenseEntity> queryable = _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        int total = await queryable.CountAsync(cancellationToken);

        List<ExpenseDto> items = await queryable
            .OrderByDescending(e => e.AddedAtUtc)
            .ThenByDescending(e => e.Id)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(x => new ExpenseDto(x.CategoryId, x.Amount, x.Comment, x.AddedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedExpenses(items, total, safePage, safeSize);
    }
}
