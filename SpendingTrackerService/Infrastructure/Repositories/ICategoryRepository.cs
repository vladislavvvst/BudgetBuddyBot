using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Infrastructure.Repositories;

internal interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryEntity>> GetActiveForUserAsync(long userId, CancellationToken cancellationToken);
    Task SeedDefaultsIfNeededAsync(long userId, CancellationToken cancellationToken);
    Task<AddCategoryResult> AddOrRestoreAsync(long userId, string name, string requestId, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(long userId, long categoryId, string requestId, CancellationToken cancellationToken);
}
