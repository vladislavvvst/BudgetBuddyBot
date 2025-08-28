namespace SpendingTrackerService.Services;

internal interface ICategorySeeder
{
    Task EnsureDefaultsAsync(long userId, CancellationToken ct);
}
