using SharedTypes.Contracts;

namespace StatisticsService.Infrastructure.Utils;

internal interface IPeriodRangeResolver
{
    bool TryResolve(PeriodsOfTime period, DateOnly? start, DateOnly? end, out DateOnlyRange range, out string? error);
}
