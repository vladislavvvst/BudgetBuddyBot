using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Database.Entities;

namespace SpendingTrackerService.Consumers;

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

        _logger.LogInformation("DeleteCategory: user={UserId}, categoryId={CategoryId}, req={RequestId}",
            request.UserId, request.CategoryId, request.RequestId);

        // Ищем категорию пользователя
        CategoryEntity? category = await _dbContext.Categories
            .SingleOrDefaultAsync(c => c.UserId == request.UserId && c.Id == request.CategoryId, ct);

        // Нет такой / уже удалена / системная — удалять нельзя
        if (category is null || category.IsDeleted || category.IsSystem)
        {
            await context.RespondAsync(new DeleteCategoryResponse(false));
            return;
        }

        try
        {
            category.IsDeleted = true;
            await _dbContext.SaveChangesAsync(ct);
            await context.RespondAsync(new DeleteCategoryResponse(true));

            _logger.LogInformation("Category soft-deleted: user={UserId}, categoryId={CategoryId}",
                request.UserId, request.CategoryId);

            IReadOnlyList<CategoryDto> items = await GetCategoriesFromDbAsync(request.UserId, ct);
            await context.Publish(new UserCategoriesChangedNotification(request.UserId, items), ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "DbUpdateException while deleting category user={UserId}, categoryId={CategoryId}",
                request.UserId, request.CategoryId);
            await context.RespondAsync(new DeleteCategoryResponse(false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting category user={UserId}, categoryId={CategoryId}",
                request.UserId, request.CategoryId);
            await context.RespondAsync(new DeleteCategoryResponse(false));
        }
    }

    private async Task<IReadOnlyList<CategoryDto>> GetCategoriesFromDbAsync(long userId, CancellationToken ct)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.IsSystem))
            .ToListAsync(ct);
    }
}
