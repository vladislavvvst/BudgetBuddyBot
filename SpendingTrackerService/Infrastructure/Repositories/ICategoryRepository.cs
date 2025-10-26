using SharedTypes.Contracts;

namespace SpendingTrackerService.Infrastructure.Repositories;

internal interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetActiveForUserAsync(long userId, CancellationToken cancellationToken);
    Task SeedDefaultsIfNeededAsync(long userId, CancellationToken cancellationToken);
    Task<AddCategoryResult> AddOrRestoreAsync(long userId, string name, string requestId, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(long userId, long categoryId, string requestId, CancellationToken cancellationToken);
    Task<Category?> GetCategoryAsync(long userId, long categoryId, CancellationToken cancellationToken);
}
