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

        if (_cache.TryGetValue(cacheKey, out UserState userState))
            return Task.FromResult(userState);

        return Task.FromResult(UserState.MainMenu);
    }

    public Task SetStateAsync(long chatId, UserState state)
    {
        string cacheKey = BuildUserStateKey(chatId);
        _cache.Set(cacheKey, state);
        return Task.CompletedTask;
    }

    // Категория, выбранная пользователем при добавлении расхода
    public Task SetCategoryIdAsync(long chatId, string value)
    {
        string cacheKey = BuildTempKey(chatId);
        _cache.Set(cacheKey, value);
        return Task.CompletedTask;
    }

    public Task<string?> GetCategoryIdAsync(long chatId)
    {
        string cacheKey = BuildTempKey(chatId);
        if (_cache.TryGetValue(cacheKey, out string? value))
            return Task.FromResult(value);

        return Task.FromResult<string?>(null);
    }

    public Task RemoveCategoryIdAsync(long chatId)
    {
        string cacheKey = BuildTempKey(chatId);
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    // Кэш списка категорий пользователя
    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(long chatId)
    {
        string key = BuildCategoriesKey(chatId);
        _cache.TryGetValue(key, out IReadOnlyList<CategoryDto>? categories);
        return Task.FromResult(categories ?? []);
    }

    public Task SetCategoriesAsync(long chatId, IReadOnlyList<CategoryDto> categories)
    {
        string cacheKey = BuildCategoriesKey(chatId);
        _cache.Set(cacheKey, categories);
        return Task.CompletedTask;
    }

    // Вспомогательные методы формирования ключей
    private static string BuildCategoriesKey(long chatId) => $"user_categories:{chatId}";
    private static string BuildUserStateKey(long chatId) => $"user_state:{chatId}";
    private static string BuildTempKey(long chatId) => $"category_id:{chatId}";
    private static string BuildDefaultCategoriesKey(long chatId) => $"default_categories_seeded:{chatId}";
}
