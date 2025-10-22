namespace StatisticsService.Infrastructure.Persistence.Entities;

internal sealed class StatsDailyEntity
{
    public long UserId { get; set; }
    public DateOnly Day { get; set; }
    public decimal AmountTotal { get; set; }
    public int ExpensesCount { get; set; }
    public decimal MaxExpenseAmount { get; set; }
    public string? MaxExpenseNote { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
