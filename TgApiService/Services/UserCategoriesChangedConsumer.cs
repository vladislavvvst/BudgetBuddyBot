using MassTransit;
using SharedTypes;
using TgApiService.Cache;

namespace TgApiService.Services;

internal class UserCategoriesChangedConsumer : IConsumer<UserCategoriesChangedNotification>
{
    private readonly ILogger<UserCategoriesChangedConsumer> _logger;
    private readonly IStateCache _stateCache;

    public UserCategoriesChangedConsumer(ILogger<UserCategoriesChangedConsumer> logger,
        IStateCache stateCache)
        => (_logger, _stateCache) = (logger, stateCache);

    public async Task Consume(ConsumeContext<UserCategoriesChangedNotification> context)
    {
        UserCategoriesChangedNotification userCategories = context.Message;
        await _stateCache.SetCategoriesAsync(userCategories.UserId, userCategories.Items);

        _logger.LogInformation(
            "Updated categories cache for user {UserId}, categoriesCount={Count}",
            userCategories.UserId,
            userCategories.Items.Count);
    }
}
