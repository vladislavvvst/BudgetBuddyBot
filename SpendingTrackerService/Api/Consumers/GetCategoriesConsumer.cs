using MassTransit;
using SharedTypes.Contracts;
using SpendingTrackerService.Infrastructure.Repositories;

namespace SpendingTrackerService.Api.Consumers;

internal sealed class GetCategoriesConsumer : IConsumer<GetCategoriesRequest>
{
    private readonly ILogger<GetCategoriesConsumer> _logger;
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesConsumer(ILogger<GetCategoriesConsumer> logger, ICategoryRepository categoryRepository)
        => (_logger, _categoryRepository) = (logger, categoryRepository);

    public async Task Consume(ConsumeContext<GetCategoriesRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        long userId = context.Message.UserId;

        try
        {
            await _categoryRepository.SeedDefaultsIfNeededAsync(userId, ct);
            IReadOnlyList<Category> items = await _categoryRepository.GetActiveForUserAsync(userId, ct);
            await context.RespondAsync(new GetCategoriesResponse(items));

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
}
