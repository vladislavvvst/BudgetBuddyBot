using MassTransit;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Repositories;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class AddExpenseConsumer : IConsumer<AddExpenseRequest>
{
    private readonly ILogger<AddExpenseConsumer> _logger;
    private readonly IExpenseRepository _expenseRepository;

    public AddExpenseConsumer(ILogger<AddExpenseConsumer> logger, IExpenseRepository expenseRepository)
        => (_logger, _expenseRepository) = (logger, expenseRepository);

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
            AddExpenseResult result = await _expenseRepository.AddAsync(
                request.UserId, request.CategoryId, amount, request.Comment, request.RequestId, ct);

            await context.RespondAsync(new AddExpenseResponse(result.Success));

            if (result.Success)
            {
                _logger.LogInformation(
                    "[AddExpense] Success user={UserId}, catId={CategoryId}, amount={Amount}, expenseId={ExpenseId}, idem={IsIdempotent}, corr={CorrelationId}, conv={ConversationId}",
                    request.UserId, request.CategoryId, amount, result.ExpenseId, result.IsIdempotent, context.CorrelationId, context.ConversationId
                );

                if (result.IsIdempotent)
                {
                    _logger.LogInformation(
                        "[AddExpense] Idempotent repeat user={UserId}, catId={CategoryId}, requestId={RequestId}",
                        request.UserId, request.CategoryId, request.RequestId);
                    return;
                }

                // При неполных данных не уведомляем сервис статистики о новых тратах -> выходим
                if (result.AddedAtUtc is null || result.CategoryEntity is null)
                {
                    _logger.LogWarning(
                        "[AddExpense] Rejected user={UserId}, time={AddedAtUtc}, category={Category}",
                        request.UserId, result.AddedAtUtc is null ? "null" : result.AddedAtUtc.ToString(), result.CategoryEntity is null ? "null" : result.CategoryEntity.Name
                    );
                    return;
                }

                await context.Publish(new ExpenseAddedNotification(request.UserId, CategoryContractMapper.ToContract(result.CategoryEntity),
                    new Expense(request.CategoryId, amount, request.Comment, result.AddedAtUtc.Value), request.RequestId), ct);
            }
            else
            {
                _logger.LogWarning(
                    "[AddExpense] Rejected user={UserId}, catId={CategoryId}, amount={Amount}, corr={CorrelationId}, conv={ConversationId}",
                    request.UserId, request.CategoryId, amount, context.CorrelationId, context.ConversationId
                );
            }
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
