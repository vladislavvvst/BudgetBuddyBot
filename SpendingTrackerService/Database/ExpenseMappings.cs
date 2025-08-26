using SharedTypes;

namespace SpendingTrackerService.Database;

internal static class ExpenseMapping
{
    public static ExpenseEntity ToEntity(this AddExpense msg) => new()
    {
        Category = msg.Category,
        Amount = msg.Amount,
        Comment = string.IsNullOrWhiteSpace(msg.Comment) ? null : msg.Comment.Trim()
    };
}
