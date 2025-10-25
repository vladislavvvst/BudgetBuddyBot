using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes.Contracts;
using StatisticsService.Infrastructure.Persistence;
using StatisticsService.Infrastructure.Persistence.Entities;

namespace StatisticsService.Api.Consumers;

internal sealed class StatsFullWeekConsumer : IConsumer<GetStatsFullWeekRequest>
{
    private readonly ILogger<StatsFullWeekConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public StatsFullWeekConsumer(ILogger<StatsFullWeekConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<GetStatsFullWeekRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        long userId = context.Message.UserId;

        // Диапазон: последние 7 календарных дней (UTC), включая сегодня
        DateOnly endDay = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly startDay = endDay.AddDays(-6);

        List<StatsDailyEntity> statsDaily = await _dbContext.StatsDaily
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Day >= startDay && x.Day <= endDay)
            .ToListAsync(ct);

        List<StatsDailyByCategoryEntity> statsDailyByCategory = await _dbContext.StatsDailyByCategory
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Day >= startDay && x.Day <= endDay)
            .ToListAsync(ct);

        // Собираем 7 дней (гарантированно) и суммы по дням
        List<DateOnly> days = Enumerable.Range(0, 7).Select(i => startDay.AddDays(i)).ToList();
        Dictionary<DateOnly, decimal> dayTotals = days.ToDictionary(d => d, _ => 0m);

        foreach (StatsDailyEntity row in statsDaily.Where(row => dayTotals.ContainsKey(row.Day)))
            dayTotals[row.Day] = row.AmountTotal;

        // Итоги
        decimal total = dayTotals.Values.Sum();
        decimal avgPerDay = total / 7m;

        // Самый затратный день
        DateOnly peakDay = default;
        decimal peakAmount = 0m;
        foreach (KeyValuePair<DateOnly, decimal> kv in dayTotals.OrderByDescending(k => k.Value))
        {
            peakDay = kv.Key;
            peakAmount = kv.Value;
            break;
        }

        DayPeakDto highestDay = new(peakDay, peakAmount);

        // Крупнейшая трата недели (берем MaxExpenseAmount среди дневных срезов)
        LargestExpenseDto largestExpense;
        StatsDailyEntity? maxExpenseRow = statsDaily
            .OrderByDescending(d => d.MaxExpenseAmount)
            .FirstOrDefault();

        if (maxExpenseRow is null || maxExpenseRow.MaxExpenseAmount <= 0m)
        {
            largestExpense = new LargestExpenseDto(0m, string.Empty, default);
        }
        else
        {
            string label = string.IsNullOrWhiteSpace(maxExpenseRow.MaxExpenseNote)
                ? GetTopCategoryNameForDay(statsDailyByCategory, maxExpenseRow.Day)
                : maxExpenseRow.MaxExpenseNote!;

            largestExpense = new LargestExpenseDto(maxExpenseRow.MaxExpenseAmount, label ?? string.Empty, maxExpenseRow.Day);
        }

        // Топ-5 категорий (по сумме за неделю)
        List<CategoryShareDto> categoriesTop5;
        if (total <= 0m || statsDailyByCategory.Count == 0)
        {
            categoriesTop5 = [];
        }
        else
        {
            List<(string Name, decimal Amount)> totalsByCategory = statsDailyByCategory
                .GroupBy(x => new { x.CategoryId, x.CategoryName })
                .Select(g => (g.Key.CategoryName, Amount: g.Sum(r => r.AmountTotal)))
                .OrderByDescending(x => x.Amount)
                .Take(5)
                .ToList();

            categoriesTop5 = totalsByCategory
                .Select(x =>
                {
                    decimal share = Math.Round(x.Amount / total * 100m, 2, MidpointRounding.AwayFromZero);
                    return new CategoryShareDto(x.Name, x.Amount, share);
                })
                .ToList();
        }

        // Крупнейшая категория (если есть данные)
        LargestCategoryDto largestCategory;
        if (categoriesTop5.Count == 0)
        {
            largestCategory = new LargestCategoryDto(string.Empty, 0m, 0m);
        }
        else
        {
            CategoryShareDto top = categoriesTop5.OrderByDescending(c => c.Amount).First();
            largestCategory = new LargestCategoryDto(top.Name, top.Amount, top.SharePercent);
        }

        // Ряд по дням (ровно 7 записей)
        List<DayAmountDto> daySeries = days
            .Select(d => new DayAmountDto(d, dayTotals[d]))
            .ToList();

        SummaryDto summary = new
        (
            Total: total,
            AvgPerDay: avgPerDay,
            LargestExpense: largestExpense,
            LargestCategory: largestCategory,
            HighestSpendingDay: highestDay
        );

        GetStatsFullWeekResponse response = new
        (
            Summary: summary,
            CategoriesTop5: categoriesTop5,
            Days: daySeries
        );

        await context.RespondAsync(response);
    }

    private static string GetTopCategoryNameForDay(List<StatsDailyByCategoryEntity> byCatRows, DateOnly day)
    {
        StatsDailyByCategoryEntity? row = byCatRows
            .Where(x => x.Day == day)
            .OrderByDescending(x => x.AmountTotal)
            .FirstOrDefault();

        return row is null ? string.Empty : row.CategoryName;
    }
}
