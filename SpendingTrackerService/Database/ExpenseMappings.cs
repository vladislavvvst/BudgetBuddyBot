using SharedTypes;

namespace SpendingTrackerService.Database;

internal static class ExpenseMapping
{
    public static ExpenseEntity ToEntity(this AddExpenseMessage msg) => new()
    {
        Category = msg.Category.ToString(),
        Amount = msg.Amount,
        Comment = string.IsNullOrWhiteSpace(msg.Comment) ? null : msg.Comment.Trim()
    };
}
