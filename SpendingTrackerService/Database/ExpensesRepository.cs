namespace SpendingTrackerService.Database;

internal class ExpensesRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ExpensesRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddExpenseAsync(ExpenseEntity expense, CancellationToken cancellationToken)
    {
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
