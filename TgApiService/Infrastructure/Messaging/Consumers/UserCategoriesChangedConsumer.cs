using MassTransit;
using SharedTypes;
using TgApiService.Application.Abstractions;

namespace TgApiService.Infrastructure.Messaging.Consumers;

/// <summary>
/// MassTransit-consumer, слушает нотификации о том, что у пользователя изменился список категорий
/// (например, категория добавлена или удалена в другом клиенте).
/// Обновляет локальный кэш категорий в StateCache, чтобы бот показывал актуальные данные.
/// </summary>
internal sealed class UserCategoriesChangedConsumer : IConsumer<UserCategoriesChangedNotification>
{
    private readonly ILogger<UserCategoriesChangedConsumer> _logger;
    private readonly IStateCache _stateCache;

    public UserCategoriesChangedConsumer(ILogger<UserCategoriesChangedConsumer> logger,
        IStateCache stateCache) => (_logger, _stateCache) = (logger, stateCache);

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
