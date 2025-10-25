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
            endDay = DateOnly.FromDateTime(DateTime.Now);
            startDay = message.Period switch
            {
                PeriodsOfTime.Day   => endDay.AddDays(-1),
                PeriodsOfTime.Week  => endDay.AddDays(-6),
                PeriodsOfTime.Month => endDay.AddDays(-30),
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
        decimal avgPerDay = total / statsDaily.Count;

        StatsDailyEntity? largestExpenseEntity = statsDaily
            .OrderByDescending(x => x.MaxExpenseAmount)
            .FirstOrDefault();

        LargestExpenseDto largestExpense = largestExpenseEntity is null
            ? LargestExpenseDto.Empty
            : new LargestExpenseDto
            (
                largestExpenseEntity.MaxExpenseAmount,
                largestExpenseEntity.MaxExpenseNote,
                largestExpenseEntity.Day
            );
        GetStatsAmountResponse response = new(Amount: total, AvgPerDay: avgPerDay, LargestExpense: largestExpense);

        await context.RespondAsync(response);
    }
}
