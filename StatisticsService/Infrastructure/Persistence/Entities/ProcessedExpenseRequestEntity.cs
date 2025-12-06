namespace StatisticsService.Infrastructure.Persistence.Entities;

internal sealed class ProcessedExpenseRequestEntity
{
    public long UserId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
