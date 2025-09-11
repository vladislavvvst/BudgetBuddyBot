using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Database.Entities;

namespace SpendingTrackerService.Consumers;

internal class EnsureUserDefaultsConsumer : IConsumer<SeedCategoriesRequest>
{
    private readonly ILogger<EnsureUserDefaultsConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public EnsureUserDefaultsConsumer(ILogger<EnsureUserDefaultsConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<SeedCategoriesRequest> context)
    {
        long userId = context.Message.UserId;
        CancellationToken ct = context.CancellationToken;

        // Какие системные категории уже есть у пользователя
        HashSet<string> existingSet = (await _dbContext.Categories
            .Where(c => c.UserId == userId && c.IsSystem && !c.IsDeleted)
            .Select(c => c.Name)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Какие надо добавить
        List<CategoryEntity> toAdd = DefaultCategories.Items
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(n => !existingSet.Contains(n))
            .Select(n => new CategoryEntity
            {
                UserId = userId,
                Name = n,
                IsSystem = true,
                IsDeleted = false,
                AddedAtUtc = DateTimeOffset.UtcNow
                // RequestId не используется для системных категорий
            }).ToList();

        if (toAdd.Count == 0)
            return;

        if (toAdd.Count > 0)
            _logger.LogInformation("Seeding {Count} system categories for user {UserId}", toAdd.Count, userId);

        _dbContext.Categories.AddRange(toAdd);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Нормальная гонка, если несколько запросов параллельно инициируют посев
            _logger.LogDebug(ex, "Race while seeding default categories for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while seeding default categories for user {UserId}", userId);
        }
    }
}
