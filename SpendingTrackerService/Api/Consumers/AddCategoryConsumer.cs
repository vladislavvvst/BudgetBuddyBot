using MassTransit;
using SharedTypes.Contracts;
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
                _logger.LogInformation(
                    "[AddCategory] {Action} '{Name}' user={UserId}, catId={CategoryId}, corr={CorrelationId}, conv={ConversationId}",
                    result.Restored ? "Restored" : "Added", request.Name, request.UserId, result.CategoryId,
                    context.CorrelationId, context.ConversationId);

                IReadOnlyList<Category> items = await _categoryRepository.GetActiveForUserAsync(request.UserId, ct);
                await context.Publish(new UserCategoriesChangedNotification(request.UserId, items), ct);
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
