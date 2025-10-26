using SharedTypes.Contracts;

namespace StatisticsService.Infrastructure.Utils;

public class PeriodRangeResolver : IPeriodRangeResolver
{
    public bool TryResolve(PeriodsOfTime period, DateOnly? start, DateOnly? end, out DateOnlyRange range, out string? error)
    {
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local).Date);

        switch (period)
        {
            case PeriodsOfTime.Day:   range = new DateOnlyRange(today, today); break;
            case PeriodsOfTime.Week:  range = new DateOnlyRange(today.AddDays(-6), today); break;
            case PeriodsOfTime.Month: range = new DateOnlyRange(today.AddDays(-29), today); break;
            case PeriodsOfTime.Custom:
                if (start is null || end is null)
                {
                    error = "StartDay/EndDay required"; range = default;
                    return false;
                }
                range = DateOnlyRange.FromInclusive(start.Value, end.Value);
                break;
            default:
                error = $"Unknown period: {period}"; range = default; return false;
        }
        error = null;
        return true;
    }
}
