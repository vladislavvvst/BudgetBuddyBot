using MassTransit;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Persistence.Entities;
using SpendingTrackerService.Infrastructure.Repositories;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class DeleteCategoryConsumer : IConsumer<DeleteCategoryRequest>
{
    private readonly ILogger<DeleteCategoryConsumer> _logger;
    private readonly ICategoryRepository _categoryRepository;

    public DeleteCategoryConsumer(ILogger<DeleteCategoryConsumer> logger, ICategoryRepository categoryRepository)
        => (_logger, _categoryRepository) = (logger, categoryRepository);

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
            DeleteCategoryResult result = await _categoryRepository.SoftDeleteAsync(request.UserId, request.CategoryId, request.RequestId, ct);
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

                IReadOnlyList<CategoryEntity> items = await _categoryRepository.GetActiveForUserAsync(request.UserId, ct);
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
}
