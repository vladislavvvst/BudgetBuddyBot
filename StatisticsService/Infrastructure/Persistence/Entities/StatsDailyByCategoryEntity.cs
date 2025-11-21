namespace StatisticsService.Infrastructure.Persistence.Entities;

internal sealed class StatsDailyByCategoryEntity
{
    public long UserId { get; set; }
    public DateOnly Day { get; set; }
    public decimal AmountTotal { get; set; }
    public decimal MaxExpenseAmount { get; set; }
    public string? MaxExpenseNote { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ExpensesCount { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
