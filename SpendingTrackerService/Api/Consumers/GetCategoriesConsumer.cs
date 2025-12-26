using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Defaults;
using SpendingTrackerService.Infrastructure.Persistence;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class GetCategoriesConsumer : IConsumer<GetCategoriesRequest>
{
    private readonly ILogger<GetCategoriesConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public GetCategoriesConsumer(ILogger<GetCategoriesConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<GetCategoriesRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        long userId = context.Message.UserId;

        try
        {
            await SeedDefaultsIfNeededAsync(userId, ct);
            IReadOnlyList<CategoryEntity> items = await GetActiveForUserAsync(userId, ct);
            await context.RespondAsync(new GetCategoriesResponse(CategoryContractMapper.ToContract(items)));

            _logger.LogInformation(
                "[GetCategories] user={UserId}, count={Count}, corr={CorrelationId}, conv={ConversationId}",
                userId, items.Count, context.CorrelationId, context.ConversationId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[GetCategories] Unexpected error user={UserId}, corr={CorrelationId}, conv={ConversationId}",
                userId, context.CorrelationId, context.ConversationId
            );

            await context.RespondAsync(new GetCategoriesResponse([]));
        }
    }

    private async Task<IReadOnlyList<CategoryEntity>> GetActiveForUserAsync(long userId, CancellationToken ct)
    {
        List<CategoryEntity> items = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return items;
    }

    private async Task SeedDefaultsIfNeededAsync(long userId, CancellationToken ct)
    {
        // Какие системные уже есть
        HashSet<string> existingSet = (await _dbContext.Categories
            .Where(c => c.UserId == userId && c.IsSystem && !c.IsDeleted)
            .Select(c => c.Name)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<CategoryEntity> toAdd = DefaultCategories.Items
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(n => !existingSet.Contains(n))
            .Select(n => new CategoryEntity
            {
                UserId = userId,
                Name = n,
                IsSystem = true,
                IsDeleted = false
                // RequestId не используется для системных
            })
            .ToList();

        if (toAdd.Count == 0)
            return;

        await _dbContext.Categories.AddRangeAsync(toAdd, ct);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            /* ignore */
        }
    }
}
