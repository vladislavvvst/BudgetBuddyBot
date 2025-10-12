using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Database.Entities;

namespace SpendingTrackerService.Consumers;

internal sealed class AddExpenseConsumer : IConsumer<AddExpenseRequest>
{
    private readonly ILogger<AddExpenseConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AddExpenseConsumer(ILogger<AddExpenseConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<AddExpenseRequest> context)
    {
        AddExpenseRequest request = context.Message;
        CancellationToken ct = context.CancellationToken;

        _logger.LogInformation("AddExpense: user={UserId}, catId={CategoryId}, amount={Amount}, req={RequestId}",
            request.UserId, request.CategoryId, request.Amount, request.RequestId);

        if (request.Amount <= 0)
        {
            await context.RespondAsync(new AddExpenseResponse(false));
            return;
        }

        // Категория должна принадлежать пользователю и быть активной
        bool categoryOk = await _dbContext.Categories
            .AnyAsync(c => c.UserId == request.UserId && c.Id == request.CategoryId && !c.IsDeleted, ct);

        if (!categoryOk)
        {
            await context.RespondAsync(new AddExpenseResponse(false));
            return;
        }

        // Идемпотентность
        bool alreadySaved = await _dbContext.Expenses
            .AnyAsync(e => e.UserId == request.UserId && e.RequestId == request.RequestId, ct);

        if (alreadySaved)
        {
            await context.RespondAsync(new AddExpenseResponse(true));
            return;
        }

        ExpenseEntity entity = new()
        {
            UserId = request.UserId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Comment = request.Comment,
            RequestId = request.RequestId
        };

        try
        {
            await _dbContext.Expenses.AddAsync(entity, ct);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Expense saved: id={Id}, amount={Amount}, catId={CategoryId}",
                entity.Id, entity.Amount, entity.CategoryId);

            await context.RespondAsync(new AddExpenseResponse(true));
        }
        catch (DbUpdateException ex)
        {
            // На случай гонки по уникальному (UserId, RequestId)
            _logger.LogWarning(ex,
                "DbUpdateException on AddExpense (maybe duplicate RequestId) user={UserId}, req={RequestId}",
                request.UserId, request.RequestId);

            bool exists = await _dbContext.Expenses
                .AnyAsync(e => e.UserId == request.UserId && e.RequestId == request.RequestId, ct);

            await context.RespondAsync(new AddExpenseResponse(exists));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving expense. Message: {@Msg}", request);
            await context.RespondAsync(new AddExpenseResponse(false));
        }
    }
}
