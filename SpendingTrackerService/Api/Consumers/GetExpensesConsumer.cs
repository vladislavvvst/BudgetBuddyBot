using MassTransit;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Repositories;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class GetExpensesConsumer : IConsumer<GetExpensesRequest>
{
    private readonly ILogger<GetExpensesConsumer> _logger;
    private readonly IExpenseRepository _expenseRepository;

    public GetExpensesConsumer(ILogger<GetExpensesConsumer> logger, IExpenseRepository expenseRepository)
        => (_logger, _expenseRepository) = (logger, expenseRepository);

    public async Task Consume(ConsumeContext<GetExpensesRequest> context)
    {
        CancellationToken ct = context.CancellationToken;

        long userId = context.Message.UserId;
        int page = Math.Max(1, context.Message.Page);
        int pageSize = Math.Clamp(context.Message.PageSize, 1, 100);

        try
        {
            PagedExpenses pageResult = await _expenseRepository.GetPagedAsync(userId, page, pageSize, ct);
            await context.RespondAsync(new GetExpensesResponse(ExpenseContractMapper.ToContract(pageResult.Items)));

            _logger.LogInformation(
                "[GetExpenses] user={UserId}, page={Page}/{PageSize}, fetched={Count}, total={Total}, corr={CorrelationId}, conv={ConversationId}",
                userId, pageResult.Page, pageResult.PageSize, pageResult.Items.Count, pageResult.Total, context.CorrelationId, context.ConversationId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[GetExpenses] Unexpected error user={UserId}, page={Page}, size={Size}, corr={CorrelationId}, conv={ConversationId}",
                userId, page, pageSize, context.CorrelationId, context.ConversationId
            );

            await context.RespondAsync(new GetExpensesResponse([]));
        }
    }
}
