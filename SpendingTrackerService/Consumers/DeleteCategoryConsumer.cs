using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Database.Entities;

namespace SpendingTrackerService.Consumers;

internal class DeleteCategoryConsumer : IConsumer<DeleteCategoryRequest>
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

            _logger.LogInformation("Category soft-deleted: user={UserId}, categoryId={CategoryId}",
                request.UserId, request.CategoryId);

            await context.RespondAsync(new DeleteCategoryResponse(true));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DbUpdateException while deleting category user={UserId}, categoryId={CategoryId}",
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
}
