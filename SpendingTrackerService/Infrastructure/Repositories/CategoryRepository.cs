using Microsoft.EntityFrameworkCore;
using Npgsql;
using SpendingTrackerService.Infrastructure.Defaults;
using SpendingTrackerService.Infrastructure.Persistence;
using SpendingTrackerService.Infrastructure.Persistence.Entities;
using System.Text.RegularExpressions;

namespace SpendingTrackerService.Infrastructure.Repositories;

internal sealed class CategoryRepository : ICategoryRepository
{
    private readonly ILogger<CategoryRepository> _logger;
    private readonly ApplicationDbContext _dbContext;

    public CategoryRepository(ILogger<CategoryRepository> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task<IReadOnlyList<CategoryEntity>> GetActiveForUserAsync(long userId, CancellationToken cancellationToken)
    {
        List<CategoryEntity> items = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task SeedDefaultsIfNeededAsync(long userId, CancellationToken cancellationToken)
    {
        // Какие системные уже есть
        HashSet<string> existingSet = (await _dbContext.Categories
            .Where(c => c.UserId == userId && c.IsSystem && !c.IsDeleted)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken))
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

        await _dbContext.Categories.AddRangeAsync(toAdd, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            /* ignore */
        }
    }

    public async Task<AddCategoryResult> AddOrRestoreAsync(long userId, string name, string requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return new AddCategoryResult(false, false, false, null);

        CategoryEntity? handled = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.RequestId == requestId)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (handled is not null)
            return new AddCategoryResult(true, false, true, handled.Id);

        // Нормализация имени
        string collapsed = Regex.Replace(name, @"\s{2,}", " ").Trim();
        if (string.IsNullOrWhiteSpace(collapsed))
            return new AddCategoryResult(false, false, false, null);

        string normalizedLower = collapsed.ToLowerInvariant();

        // Активная с таким именем уже есть?
        bool activeExists = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId
                           && !c.IsDeleted
                           && c.Name == normalizedLower, cancellationToken);
        if (activeExists)
            return new AddCategoryResult(false, false, false, null);

        // Попробуем восстановить последнюю удаленную с таким именем
        CategoryEntity? deleted = await _dbContext.Categories
            .Where(c => c.UserId == userId
                        && c.IsDeleted
                        && !c.IsSystem
                        && c.Name == normalizedLower)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (deleted is not null)
        {
            // Дополнительная проверка гонки по имени (между чтением и апдейтом)
            bool conflict = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(c => c.UserId == userId
                               && !c.IsDeleted
                               && !c.IsSystem
                               && c.Name == normalizedLower, cancellationToken);
            if (conflict)
                return new AddCategoryResult(false, false, false, null);

            // Восстановление на стороне БД
            await _dbContext.Categories
                .Where(c => c.Id == deleted.Id && c.UserId == userId && c.IsDeleted && !c.IsSystem)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsDeleted, false)
                    .SetProperty(c => c.RequestId, requestId), cancellationToken);

            return new AddCategoryResult(true, true, false, deleted.Id);
        }

        CategoryEntity entity = new()
        {
            UserId = userId,
            Name = normalizedLower,
            IsSystem = false,
            IsDeleted = false,
            RequestId = requestId
        };

        try
        {
            await _dbContext.Categories.AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new AddCategoryResult(true, false, false, entity.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            CategoryEntity? existing = await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .FirstOrDefaultAsync(c => c.RequestId == requestId || c.Name == normalizedLower, cancellationToken);

            if (existing is null)
                return new AddCategoryResult(false, false, false, null);

            bool isIdempotent = string.Equals(existing.RequestId, requestId, StringComparison.Ordinal);
            return new AddCategoryResult(true, false, isIdempotent, existing.Id);
        }
    }

    public async Task<DeleteCategoryResult> SoftDeleteAsync(long userId, long categoryId, string requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return new DeleteCategoryResult(false, false);

        bool alreadyHandled = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.RequestId == requestId, cancellationToken);

        if (alreadyHandled)
            return new DeleteCategoryResult(true, true);

        // Удаляем на стороне БД с условиями не системная, не удалена
        int rows = await _dbContext.Categories
            .Where(c => c.UserId == userId
                        && c.Id == categoryId
                        && !c.IsSystem
                        && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.RequestId, requestId), cancellationToken);

        if (rows == 1)
            return new DeleteCategoryResult(true, false);

        // Если не обновили ни одной строки, проверим - возможно, это повтор той же команды (RequestId уже записан)
        bool handledNow = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.RequestId == requestId, cancellationToken);

        return new DeleteCategoryResult(handledNow, handledNow);
    }
}
