using MassTransit;
using SharedTypes;
using SpendingTrackerService.Infrastructure.Repositories;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class DeleteCategoryConsumer : IConsumer<DeleteCategoryRequest>
{
    private readonly ILogger<DeleteCategoryConsumer> _logger;
    private readonly ICategoryRepository _categories;

    public DeleteCategoryConsumer(ILogger<DeleteCategoryConsumer> logger, ICategoryRepository categories)
        => (_logger, _categories) = (logger, categories);

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
            bool success = await _categories.SoftDeleteAsync(request.UserId, request.CategoryId, request.RequestId, ct);
            await context.RespondAsync(new DeleteCategoryResponse(success));

            if (success)
            {
                _logger.LogInformation(
                    "[DeleteCategory] Soft-deleted user={UserId}, categoryId={CategoryId}, corr={CorrelationId}, conv={ConversationId}",
                    request.UserId, request.CategoryId, context.CorrelationId, context.ConversationId
                );

                IReadOnlyList<CategoryDto> items = await _categories.GetActiveForUserAsync(request.UserId, ct);
                await context.Publish(new UserCategoriesChangedNotification(request.UserId, items), ct);
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
}
