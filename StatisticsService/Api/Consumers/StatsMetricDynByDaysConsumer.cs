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

        DateOnly endDay;
        DateOnly startDay;

        if (message.Period == PeriodsOfTime.Custom)
        {
            // Разобрать даты из message.StartDate и message.EndDate
            if (message.StartDay is null)
            {
                _logger.LogInformation("Invalid message. StartDay is null");
                await context.RespondAsync(GetStatsAmountResponse.Empty);
                return;
            }

            if (message.EndDay is null)
            {
                _logger.LogInformation("Invalid message. EndDay is null");
                await context.RespondAsync(GetStatsAmountResponse.Empty);
                return;
            }

            startDay = message.StartDay.Value;
            endDay   = message.EndDay.Value;
        }
        else
        {
            endDay = DateOnly.FromDateTime(DateTime.Now);
            startDay = message.Period switch
            {
                PeriodsOfTime.Day   => endDay,
                PeriodsOfTime.Week  => endDay.AddDays(-6),
                PeriodsOfTime.Month => endDay.AddDays(-29),
                _ => throw new InvalidEnumArgumentException(nameof(context), (int)message.Period, typeof(PeriodsOfTime))
            };
        }

        List<DailyAmount> daysAmount = await _dbContext.StatsDaily
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Day >= startDay && x.Day <= endDay)
            .Select(x => new DailyAmount(x.Day, x.AmountTotal))
            .ToListAsync(ct);

        await context.RespondAsync(new GetStatsDaysResponse(daysAmount));
    }
}
