using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;

namespace SpendingTrackerService.Services;

internal class AddExpenseConsumer : IConsumer<AddExpense>
{
    private readonly ILogger<AddExpenseConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AddExpenseConsumer(ILogger<AddExpenseConsumer> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<AddExpense> context)
    {
        _logger.LogInformation("Received message: {Message}", context.Message);

        try
        {
            ExpenseEntity entity = context.Message.ToEntity();
            await _dbContext.Expenses.AddAsync(entity);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Expense saved: {Id} {Amount} {Category}", entity.Id, entity.Amount, entity.Category);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "DB error while saving expense. Message: {@Message}", context.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving expense. Message: {@Message}", context.Message);
        }

        await Task.CompletedTask;
    }
}
