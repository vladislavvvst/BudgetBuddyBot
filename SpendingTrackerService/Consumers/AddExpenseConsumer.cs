using MassTransit;
using SharedTypes;
using SpendingTrackerService.Database.Repository;

namespace SpendingTrackerService.Consumers;

internal sealed class AddExpenseConsumer : IConsumer<AddExpenseRequest>
{
    private readonly ILogger<AddExpenseConsumer> _logger;
    private readonly IExpenseRepository _expenses;

    public AddExpenseConsumer(ILogger<AddExpenseConsumer> logger, IExpenseRepository expenses)
        => (_logger, _expenses) = (logger, expenses);

    public async Task Consume(ConsumeContext<AddExpenseRequest> context)
    {
        AddExpenseRequest request = context.Message;
        CancellationToken ct = context.CancellationToken;

        if (string.IsNullOrWhiteSpace(request.RequestId) || request.Amount <= 0m || request.CategoryId <= 0)
        {
            await context.RespondAsync(new AddExpenseResponse(false));
            return;
        }

        try
        {
            AddExpenseResult result = await _expenses.AddAsync
            (
                request.UserId,
                request.CategoryId,
                request.Amount,
                request.Comment,
                request.RequestId,
                ct
            );

            await context.RespondAsync(new AddExpenseResponse(result.Success));

            if (result.Success)
            {
                _logger.LogInformation(
                    "[AddExpense] Success user={UserId}, catId={CategoryId}, amount={Amount}, expenseId={ExpenseId}, idem={IsIdempotent}, corr={CorrelationId}, conv={ConversationId}",
                    request.UserId,
                    request.CategoryId,
                    request.Amount,
                    result.ExpenseId,
                    result.IsIdempotent,
                    context.CorrelationId,
                    context.ConversationId
                );
            }
            else
            {
                _logger.LogWarning(
                    "[AddExpense] Rejected user={UserId}, catId={CategoryId}, amount={Amount}, corr={CorrelationId}, conv={ConversationId}",
                    request.UserId,
                    request.CategoryId,
                    request.Amount,
                    context.CorrelationId,
                    context.ConversationId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddExpense] Unexpected error user={UserId}, catId={CategoryId}, amount={Amount}, corr={CorrelationId}, conv={ConversationId}",
                request.UserId,
                request.CategoryId,
                request.Amount,
                context.CorrelationId,
                context.ConversationId);

            await context.RespondAsync(new AddExpenseResponse(false));
        }
    }
}
