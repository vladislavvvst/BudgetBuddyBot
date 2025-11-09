using SharedTypes.Contracts;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Api.Mapping;

internal static class CategoryContractMapper
{
    public static Category ToContract(CategoryEntity categoryEntity)
    {
        Category result = new
        (
            Id: categoryEntity.Id,
            Name: categoryEntity.Name,
            IsSystem: categoryEntity.IsSystem
        );
        return result;
    }

    public static IReadOnlyList<Category> ToContract(IReadOnlyList<CategoryEntity> categoryEntities)
    {
        List<Category> list = new(categoryEntities.Count);
        list.AddRange(categoryEntities.Select(ToContract));
        return list;
    }
}
