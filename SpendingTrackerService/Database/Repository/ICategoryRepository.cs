using SharedTypes;

namespace SpendingTrackerService.Database.Repository;

internal interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryDto>> GetActiveForUserAsync(long userId, CancellationToken cancellationToken);
    Task SeedDefaultsIfNeededAsync(long userId, CancellationToken cancellationToken);
    Task<AddCategoryResult> AddOrRestoreAsync(long userId, string name, string requestId, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(long userId, long categoryId, string requestId, CancellationToken cancellationToken);
}
