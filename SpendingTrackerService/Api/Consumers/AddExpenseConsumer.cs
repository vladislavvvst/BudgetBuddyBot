using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Persistence;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Api.Consumers;

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

        if (string.IsNullOrWhiteSpace(request.RequestId) || request.Amount <= 0m || request.CategoryId <= 0)
        {
            await context.RespondAsync(new AddExpenseResponse(false));
            return;
        }

        // Округляем до 2 знаков
        decimal amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

        try
        {
            bool alreadyHandled = await _dbContext.Expenses
                .AsNoTracking()
                .AnyAsync(e => e.UserId == request.UserId && e.RequestId == request.RequestId, ct);

            if (alreadyHandled)
            {
                await context.RespondAsync(new AddExpenseResponse(true));
                _logger.LogInformation(
                    "[AddExpense] Idempotent repeat user={UserId}, catId={CategoryId}, requestId={RequestId}",
                    request.UserId, request.CategoryId, request.RequestId);
                return;
            }

            CategoryEntity? categoryEntity = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.Id == request.CategoryId && !c.IsDeleted, ct);

            if (categoryEntity is null)
            {
                await context.RespondAsync(new AddExpenseResponse(false));
                _logger.LogWarning(
                    "[AddExpense] Rejected user={UserId}, catId={CategoryId}, amount={Amount}, corr={CorrelationId}, conv={ConversationId}",
                    request.UserId, request.CategoryId, amount, context.CorrelationId, context.ConversationId
                );
                return;
            }

            DateTimeOffset addedAtUtc = DateTimeOffset.UtcNow;
            ExpenseEntity entity = new()
            {
                UserId = request.UserId,
                CategoryId = request.CategoryId,
                Amount = amount,
                Comment = request.Comment,
                RequestId = request.RequestId,
                AddedAtUtc = addedAtUtc.UtcDateTime
            };

            await _dbContext.Expenses.AddAsync(entity, ct);

            await context.Publish(new ExpenseAddedNotification(request.UserId, CategoryContractMapper.ToContract(categoryEntity),
                new Expense(request.CategoryId, amount, request.Comment, addedAtUtc), request.RequestId), ct);

            await _dbContext.SaveChangesAsync(ct);

            await context.RespondAsync(new AddExpenseResponse(true));

            _logger.LogInformation(
                "[AddExpense] Success user={UserId}, catId={CategoryId}, amount={Amount}, expenseId={ExpenseId}, idem={IsIdempotent}, corr={CorrelationId}, conv={ConversationId}",
                request.UserId, request.CategoryId, amount, entity.Id, false, context.CorrelationId, context.ConversationId
            );
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            ExpenseEntity? existing = await _dbContext.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == request.UserId && e.RequestId == request.RequestId, ct);

            if (existing is null)
            {
                await context.RespondAsync(new AddExpenseResponse(false));
                return;
            }

            await context.RespondAsync(new AddExpenseResponse(true));

            _logger.LogInformation(
                "[AddExpense] Idempotent repeat user={UserId}, catId={CategoryId}, requestId={RequestId}",
                request.UserId, existing.CategoryId, request.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddExpense] Unexpected error user={UserId}, catId={CategoryId}, amount={Amount}, corr={CorrelationId}, conv={ConversationId}",
                request.UserId, request.CategoryId, amount, context.CorrelationId, context.ConversationId);

            await context.RespondAsync(new AddExpenseResponse(false));
        }
    }
}
