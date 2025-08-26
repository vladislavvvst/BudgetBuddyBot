using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;

namespace SpendingTrackerService.Services;

internal class GetExpensesConsumer : IConsumer<GetExpenses>
{
    private readonly ILogger<GetExpensesConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public GetExpensesConsumer(ILogger<GetExpensesConsumer> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<GetExpenses> context)
    {
        int page = Math.Max(1, context.Message.Page);
        int pageSize = Math.Clamp(context.Message.PageSize, 1, 100);

        var q = _dbContext.Expenses.AsNoTracking().OrderByDescending(e => e.AddDate);

        int total = await q.CountAsync(context.CancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
              .Select(x => new ExpenseDto(x.Category, x.Amount, x.Comment, x.AddDate))
              .ToListAsync(context.CancellationToken);

        _logger.LogInformation("Fetched {Count} expenses (page {Page} of size {PageSize})", items.Count, page, pageSize);

        await context.RespondAsync<ExpensesPage>(new(total, page, pageSize, items));
    }
}
