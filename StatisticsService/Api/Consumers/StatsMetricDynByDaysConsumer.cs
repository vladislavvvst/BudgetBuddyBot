using System.ComponentModel;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes.Contracts;
using StatisticsService.Infrastructure.Persistence;

namespace StatisticsService.Api.Consumers;

internal sealed class StatsMetricDynByDaysConsumer : IConsumer<GetStatsDaysRequest>
{
    private readonly ILogger<StatsMetricDynByDaysConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public StatsMetricDynByDaysConsumer(ILogger<StatsMetricDynByDaysConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<GetStatsDaysRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        GetStatsDaysRequest message = context.Message;
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
            await context.RespondAsync(new GetStatsDaysResponse([]));
            return;
        }

        // Инициализируем ряд всеми днями с нулями, чтобы на фронте не было дыр
        int daysCount = endDay.DayNumber - startDay.DayNumber + 1;
        Dictionary<DateOnly, decimal> totals = Enumerable
            .Range(0, daysCount)
            .Select(offset => startDay.AddDays(offset))
            .ToDictionary(d => d, _ => 0m);

        List<DailyAmount> rows = await _dbContext.StatsDaily
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Day >= startDay && x.Day <= endDay)
            .Select(x => new DailyAmount(x.Day, x.AmountTotal))
            .ToListAsync(ct);

        foreach (DailyAmount row in rows)
            totals[row.Day] = row.Amount;

        List<DailyAmount> ordered = totals
            .OrderBy(kv => kv.Key)
            .Select(kv => new DailyAmount(kv.Key, kv.Value))
            .ToList();

        await context.RespondAsync(new GetStatsDaysResponse(ordered));
    }
}
