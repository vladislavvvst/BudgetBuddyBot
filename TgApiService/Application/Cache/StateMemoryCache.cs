using Microsoft.Extensions.Caching.Memory;
using SharedTypes;

namespace TgApiService.Application.Cache;

/// <summary>
/// In-memory IStateCache.
/// Держит состояние и данные пользователей в памяти процесса.
/// </summary>
internal class StateMemoryCache : IStateCache
{
    private readonly IMemoryCache _cache;

    public StateMemoryCache(IMemoryCache cache)
        => _cache = cache;

    // Состояния пользователя
    public Task<UserState> GetStateAsync(long chatId)
    {
        string cacheKey = BuildUserStateKey(chatId);
        return Task.FromResult(_cache.TryGetValue(cacheKey, out UserState userState)
            ? userState
            : UserState.MainMenu);
    }

    public Task SetStateAsync(long chatId, UserState state)
    {
        string cacheKey = BuildUserStateKey(chatId);
        _cache.Set(cacheKey, state);
        return Task.CompletedTask;
    }

    // Категория, выбранная пользователем при добавлении расхода
    public Task SetCategoryIdAsync(long chatId, long categoryId)
    {
        string cacheKey = BuildCategoryChooseKey(chatId);
        _cache.Set(cacheKey, categoryId);
        return Task.CompletedTask;
    }

    public Task<long?> GetCategoryIdAsync(long chatId)
    {
        string cacheKey = BuildCategoryChooseKey(chatId);
        return _cache.TryGetValue(cacheKey, out long? categoryId)
            ? Task.FromResult(categoryId)
            : Task.FromResult<long?>(null);
    }

    public Task RemoveCategoryIdAsync(long chatId)
    {
        string cacheKey = BuildCategoryChooseKey(chatId);
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    // Кэш списка категорий пользователя
    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(long chatId)
    {
        string cacheKey = BuildCategoriesKey(chatId);
        _cache.TryGetValue(cacheKey, out IReadOnlyList<CategoryDto>? categories);
        return Task.FromResult(categories ?? []);
    }

    public Task SetCategoriesAsync(long chatId, IReadOnlyList<CategoryDto> categories)
    {
        string cacheKey = BuildCategoriesKey(chatId);
        _cache.Set(cacheKey, categories);
        return Task.CompletedTask;
    }

    // Временно выбранный период для статистики
    public Task<string?> GetStatsPeriod(long chatId)
    {
        string cacheKey = BuildStatsPeriodKey(chatId);
        _cache.TryGetValue(cacheKey, out string? period);
        return Task.FromResult(period);
    }

    public Task SetStatsPeriod(long chatId, string period)
    {
        string cacheKey = BuildStatsPeriodKey(chatId);
        _cache.Set(cacheKey, period);
        return Task.CompletedTask;
    }

    public Task RemoveStatsPeriod(long chatId)
    {
        string cacheKey = BuildStatsPeriodKey(chatId);
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    // Вспомогательные методы формирования ключей
    private static string BuildCategoriesKey(long chatId) => $"user_categories:{chatId}";
    private static string BuildUserStateKey(long chatId) => $"user_state:{chatId}";
    private static string BuildCategoryChooseKey(long chatId) => $"category_choose:{chatId}";
    private static string BuildStatsPeriodKey(long chatId) => $"stats_period:{chatId}";
}
