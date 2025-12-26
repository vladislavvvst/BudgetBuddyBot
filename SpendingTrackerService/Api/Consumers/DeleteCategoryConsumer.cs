using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Persistence;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class DeleteCategoryConsumer : IConsumer<DeleteCategoryRequest>
{
    private readonly ILogger<DeleteCategoryConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public DeleteCategoryConsumer(ILogger<DeleteCategoryConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<DeleteCategoryRequest> context)
    {
        DeleteCategoryRequest request = context.Message;
        CancellationToken ct = context.CancellationToken;

        if (string.IsNullOrWhiteSpace(request.RequestId) || request.CategoryId <= 0)
        {
            await context.RespondAsync(new DeleteCategoryResponse(false));
            return;
        }

        try
        {
            DeleteCategoryResult result = await SoftDeleteAsync(request.UserId, request.CategoryId, request.RequestId, ct);
            await context.RespondAsync(new DeleteCategoryResponse(result.Success));

            if (result.Success)
            {
                if (result.IsIdempotent)
                {
                    _logger.LogInformation(
                        "[DeleteCategory] Idempotent repeat user={UserId}, categoryId={CategoryId}, requestId={RequestId}, corr={CorrelationId}, conv={ConversationId}",
                        request.UserId, request.CategoryId, request.RequestId, context.CorrelationId, context.ConversationId);
                    return;
                }

                _logger.LogInformation(
                    "[DeleteCategory] Soft-deleted user={UserId}, categoryId={CategoryId}, corr={CorrelationId}, conv={ConversationId}",
                    request.UserId, request.CategoryId, context.CorrelationId, context.ConversationId
                );

                IReadOnlyList<CategoryEntity> items = await GetActiveForUserAsync(request.UserId, ct);
                await context.Publish(new UserCategoriesChangedNotification(request.UserId, CategoryContractMapper.ToContract(items)), ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[DeleteCategory] Unexpected error user={UserId}, categoryId={CategoryId}, corr={CorrelationId}, conv={ConversationId}",
                request.UserId, request.CategoryId, context.CorrelationId, context.ConversationId);

            await context.RespondAsync(new DeleteCategoryResponse(false));
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

    private async Task<DeleteCategoryResult> SoftDeleteAsync(long userId, long categoryId, string requestId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return new DeleteCategoryResult(false, false);

        bool alreadyHandled = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.RequestId == requestId, ct);

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
                .SetProperty(c => c.RequestId, requestId), ct);

        if (rows == 1)
            return new DeleteCategoryResult(true, false);

        // Если не обновили ни одной строки, проверим - возможно, это повтор той же команды (RequestId уже записан)
        bool handledNow = await _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.RequestId == requestId, ct);

        return new DeleteCategoryResult(handledNow, handledNow);
    }

    private sealed record DeleteCategoryResult(bool Success, bool IsIdempotent);
}
