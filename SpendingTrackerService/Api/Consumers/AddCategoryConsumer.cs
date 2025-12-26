using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Persistence;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class AddCategoryConsumer : IConsumer<AddCategoryRequest>
{
    private readonly ILogger<AddCategoryConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AddCategoryConsumer(ILogger<AddCategoryConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<AddCategoryRequest> context)
    {
        AddCategoryRequest request = context.Message;
        CancellationToken ct = context.CancellationToken;

        if (string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.Name))
        {
            await context.RespondAsync(new AddCategoryResponse(false));
            return;
        }

        try
        {
            AddCategoryResult result = await AddOrRestoreAsync(request.UserId, request.Name, request.RequestId, ct);
            await context.RespondAsync(new AddCategoryResponse(result.Success));

            if (result.Success)
            {
                if (result.IsIdempotent)
                {
                    _logger.LogInformation(
                        "[AddCategory] Idempotent repeat '{Name}' user={UserId}, catId={CategoryId}, requestId={RequestId}, corr={CorrelationId}, conv={ConversationId}",
                        request.Name, request.UserId, result.CategoryId, request.RequestId, context.CorrelationId, context.ConversationId);
                    return;
                }

                _logger.LogInformation(
                    "[AddCategory] {Action} '{Name}' user={UserId}, catId={CategoryId}, corr={CorrelationId}, conv={ConversationId}",
                    result.Restored ? "Restored" : "Added", request.Name, request.UserId, result.CategoryId,
                    context.CorrelationId, context.ConversationId);

                IReadOnlyList<CategoryEntity> items = await GetActiveForUserAsync(request.UserId, ct);
                await context.Publish(new UserCategoriesChangedNotification(request.UserId, CategoryContractMapper.ToContract(items)), ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddCategory] Unexpected error user={UserId}, name='{Name}', corr={CorrelationId}, conv={ConversationId}",
                request.UserId, request.Name, context.CorrelationId, context.ConversationId);

            await context.RespondAsync(new AddCategoryResponse(false));
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

    private async Task<AddCategoryResult> AddOrRestoreAsync(long userId, string name, string requestId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return new AddCategoryResult(false, false, false, null);

        CategoryEntity? handled = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.RequestId == requestId)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(ct);

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
                           && c.Name == normalizedLower, ct);
        if (activeExists)
            return new AddCategoryResult(false, false, false, null);

        // Попробуем восстановить последнюю удаленную с таким именем
        CategoryEntity? deleted = await _dbContext.Categories
            .Where(c => c.UserId == userId
                        && c.IsDeleted
                        && !c.IsSystem
                        && c.Name == normalizedLower)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (deleted is not null)
        {
            // Дополнительная проверка гонки по имени (между чтением и апдейтом)
            bool conflict = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(c => c.UserId == userId
                               && !c.IsDeleted
                               && !c.IsSystem
                               && c.Name == normalizedLower, ct);
            if (conflict)
                return new AddCategoryResult(false, false, false, null);

            // Восстановление на стороне БД
            await _dbContext.Categories
                .Where(c => c.Id == deleted.Id && c.UserId == userId && c.IsDeleted && !c.IsSystem)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsDeleted, false)
                    .SetProperty(c => c.RequestId, requestId), ct);

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
            await _dbContext.Categories.AddAsync(entity, ct);
            await _dbContext.SaveChangesAsync(ct);
            return new AddCategoryResult(true, false, false, entity.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            CategoryEntity? existing = await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .FirstOrDefaultAsync(c => c.RequestId == requestId || c.Name == normalizedLower, ct);

            if (existing is null)
                return new AddCategoryResult(false, false, false, null);

            bool isIdempotent = string.Equals(existing.RequestId, requestId, StringComparison.Ordinal);
            return new AddCategoryResult(true, false, isIdempotent, existing.Id);
        }
    }

    private sealed record AddCategoryResult(bool Success, bool Restored, bool IsIdempotent, long? CategoryId);
}
