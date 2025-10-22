using SharedTypes;

namespace SpendingTrackerService.Infrastructure.Repositories;

internal interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryDto>> GetActiveForUserAsync(long userId, CancellationToken cancellationToken);
    Task SeedDefaultsIfNeededAsync(long userId, CancellationToken cancellationToken);
    Task<AddCategoryResult> AddOrRestoreAsync(long userId, string name, string requestId, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(long userId, long categoryId, string requestId, CancellationToken cancellationToken);
    Task<CategoryDto?> GetCategoryAsync(long userId, long categoryId, CancellationToken cancellationToken);
}
