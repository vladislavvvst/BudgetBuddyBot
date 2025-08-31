using Microsoft.Extensions.Caching.Memory;

namespace TgApiService.Services;

internal class MemoryUserStateStorage : IUserStateStorage
{
    private readonly IMemoryCache _cache;

    public MemoryUserStateStorage(IMemoryCache cache)
    {
        _cache = cache;
    }

    // Основные состояния пользователя
    public Task<UserState> GetStateAsync(long chatId)
    {
        if (_cache.TryGetValue(chatId, out UserState userState))
            return Task.FromResult(userState);

        return Task.FromResult(UserState.MainMenu);
    }

    public Task SetStateAsync(long chatId, UserState state)
    {
        _cache.Set(chatId, state);
        return Task.CompletedTask;
    }

    // Временные значения (per-user, per-key)
    public Task SetTempAsync(long chatId, string key, string value)
    {
        string cacheKey = BuildTempKey(chatId, key);
        _cache.Set(cacheKey, value);
        return Task.CompletedTask;
    }

    public Task<string?> GetTempAsync(long chatId, string key)
    {
        string cacheKey = BuildTempKey(chatId, key);
        if (_cache.TryGetValue(cacheKey, out string? value))
            return Task.FromResult<string?>(value);

        return Task.FromResult<string?>(null);
    }

    public Task RemoveTempAsync(long chatId, string key)
    {
        string cacheKey = BuildTempKey(chatId, key);
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    private static string BuildTempKey(long chatId, string key) => $"temp:{chatId}:{key}";
}
