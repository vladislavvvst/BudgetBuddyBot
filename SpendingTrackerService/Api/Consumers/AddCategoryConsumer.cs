using MassTransit;
using SharedTypes.Contracts;
using SpendingTrackerService.Api.Mapping;
using SpendingTrackerService.Infrastructure.Persistence.Entities;
using SpendingTrackerService.Infrastructure.Repositories;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class AddCategoryConsumer : IConsumer<AddCategoryRequest>
{
    private readonly ILogger<AddCategoryConsumer> _logger;
    private readonly ICategoryRepository _categoryRepository;

    public AddCategoryConsumer(ILogger<AddCategoryConsumer> logger, ICategoryRepository categoryRepository)
        => (_logger, _categoryRepository) = (logger, categoryRepository);

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
            AddCategoryResult result = await _categoryRepository.AddOrRestoreAsync(request.UserId, request.Name, request.RequestId, ct);
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

                IReadOnlyList<CategoryEntity> items = await _categoryRepository.GetActiveForUserAsync(request.UserId, ct);
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
}
