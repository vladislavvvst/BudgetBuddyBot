using System.ComponentModel;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes.Contracts;
using StatisticsService.Infrastructure.Persistence;

namespace StatisticsService.Api.Consumers;

internal sealed class StatsMetricByCategoryConsumer : IConsumer<GetStatsTopCategoryRequest>
{
    private readonly ILogger<StatsMetricByCategoryConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public StatsMetricByCategoryConsumer(ILogger<StatsMetricByCategoryConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<GetStatsTopCategoryRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        GetStatsTopCategoryRequest message = context.Message;
        long userId = context.Message.UserId;

        DateOnly endDay = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly startDay = message.Period switch
        {
            PeriodsOfTime.Day   => endDay,
            PeriodsOfTime.Week  => endDay.AddDays(-6),
            PeriodsOfTime.Month => endDay.AddDays(-29),
            _ => throw new InvalidEnumArgumentException(nameof(context), (int)message.Period, typeof(PeriodsOfTime))
        };

        if (startDay > endDay)
        {
            _logger.LogWarning("Invalid range: start {StartDay} > end {EndDay}, userId {UserId}", startDay, endDay, userId);
            await context.RespondAsync(GetStatsTopCategoryResponse.Empty);
            return;
        }

        // Составляем список формата [Имя категории] : [Потраченная сумма за период]
        List<TotalSpendByCategory> categories = (await _dbContext.StatsDailyByCategory
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Day >= startDay && s.Day <= endDay)
            .GroupBy(s => s.CategoryName)
            .Select(g => new { Name = g.Key, Amount = g.Sum(s => s.AmountTotal) })
            .OrderByDescending(x => x.Amount)
            .ThenBy(x => x.Name)
            .ToListAsync(ct))
                .Select(x => new TotalSpendByCategory(x.Name, x.Amount))
                .ToList();

        await context.RespondAsync(new GetStatsTopCategoryResponse(categories));
    }
}
