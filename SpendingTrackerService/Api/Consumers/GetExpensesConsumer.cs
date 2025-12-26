using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Persistence;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class GetExpensesConsumer : IConsumer<GetExpensesRequest>
{
    private readonly ILogger<GetExpensesConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public GetExpensesConsumer(ILogger<GetExpensesConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<GetExpensesRequest> context)
    {
        CancellationToken ct = context.CancellationToken;

        long userId = context.Message.UserId;
        int page = Math.Max(1, context.Message.Page);
        int pageSize = Math.Clamp(context.Message.PageSize, 1, 100);

        try
        {
            IQueryable<ExpenseEntity> queryable = _dbContext.Expenses
                .AsNoTracking()
                .Where(e => e.UserId == userId);

            int total = await queryable.CountAsync(ct);

            List<ExpenseEntity> items = await queryable
                .OrderByDescending(e => e.AddedAtUtc)
                .ThenByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            await context.RespondAsync(new GetExpensesResponse(ExpenseContractMapper.ToContract(items)));

            _logger.LogInformation(
                "[GetExpenses] user={UserId}, page={Page}/{PageSize}, fetched={Count}, total={Total}, corr={CorrelationId}, conv={ConversationId}",
                userId, page, pageSize, items.Count, total, context.CorrelationId, context.ConversationId
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
