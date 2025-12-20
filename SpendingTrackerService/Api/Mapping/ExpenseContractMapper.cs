using SharedTypes.Contracts;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Api.Mapping;

internal static class ExpenseContractMapper
{
    public static Expense ToContract(ExpenseEntity expenseEntity)
    {
        Expense result = new
        (
            CategoryId: expenseEntity.CategoryId,
            Amount: expenseEntity.Amount,
            Comment: expenseEntity.Comment,
            AddedAtUtc: expenseEntity.AddedAtUtc
        );
        return result;
    }

    public static IReadOnlyList<Expense> ToContract(IReadOnlyList<ExpenseEntity> expenseEntities)
    {
        List<Expense> list = new(expenseEntities.Count);
        list.AddRange(expenseEntities.Select(ToContract));
        return list;
    }
}
