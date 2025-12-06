using System.ComponentModel;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes.Contracts;
using StatisticsService.Infrastructure.Persistence;
using StatisticsService.Infrastructure.Persistence.Entities;

namespace StatisticsService.Api.Consumers;

internal sealed class StatsMetricTotalAmountConsumer : IConsumer<GetStatsAmountRequest>
{
    private readonly ILogger<StatsMetricTotalAmountConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public StatsMetricTotalAmountConsumer(ILogger<StatsMetricTotalAmountConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<GetStatsAmountRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        GetStatsAmountRequest message = context.Message;
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
            endDay = DateOnly.FromDateTime(DateTime.UtcNow);
            startDay = message.Period switch
            {
                PeriodsOfTime.Day   => endDay,
                PeriodsOfTime.Week  => endDay.AddDays(-6),
                PeriodsOfTime.Month => endDay.AddDays(-29),
                _ => throw new InvalidEnumArgumentException(nameof(context), (int)message.Period, typeof(PeriodsOfTime))
            };
        }

        List<StatsDailyEntity> statsDaily = await _dbContext.StatsDaily
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Day >= startDay && x.Day <= endDay)
            .ToListAsync(ct);

        if (statsDaily.Count == 0)
        {
            _logger.LogWarning("No stats data found for user {UserId}", userId);
            await context.RespondAsync(GetStatsAmountResponse.Empty);
            return;
        }

        decimal total = statsDaily.Sum(x => x.AmountTotal);
        // Среднее за календарные дни периода, а не только за дни с расходами
        int daysInRange = endDay.DayNumber - startDay.DayNumber + 1;
        decimal avgPerDay = total / daysInRange;

        StatsDailyEntity? largestExpenseEntity = statsDaily
            .OrderByDescending(x => x.MaxExpenseAmount)
            .FirstOrDefault();

        LargestExpenseDay largestExpenseDay = largestExpenseEntity is null
            ? LargestExpenseDay.Empty
            : new LargestExpenseDay
            (
                largestExpenseEntity.MaxExpenseAmount,
                largestExpenseEntity.MaxExpenseNote,
                largestExpenseEntity.Day
            );
        GetStatsAmountResponse response = new(Amount: total, AvgPerDay: avgPerDay, LargestExpenseDay: largestExpenseDay);

        await context.RespondAsync(response);
    }
}
