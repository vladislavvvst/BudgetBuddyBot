namespace SpendingTrackerService.Database;

internal class ExpenseEntity
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset AddDate { get; set; }

}
