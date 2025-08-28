using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;

namespace SpendingTrackerService.Consumers;

internal class GetExpensesConsumer : IConsumer<GetExpensesRequest>
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

        // Базовый запрос по пользователю
        var queryable = _dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.AddedAtUtc);

        int total = await queryable.CountAsync(ct);

        // Пагинация
        List<ExpenseDto> items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ExpenseDto
            (
                x.CategoryId,
                x.Amount,
                x.Comment,
                x.AddedAtUtc)
            ).ToListAsync(ct);

        _logger.LogInformation("Fetched {Count} expenses for user {UserId} (page {Page} size {Size} of total {Total})",
            items.Count, userId, page, pageSize, total);

        GetExpensesResponse response = new(userId, items, total);
        await context.RespondAsync(response);
    }
}
