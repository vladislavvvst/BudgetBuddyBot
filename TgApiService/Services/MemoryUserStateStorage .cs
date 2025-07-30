using Microsoft.Extensions.Caching.Memory;

namespace TgApiService.Services;

internal class MemoryUserStateStorage : IUserStateStorage
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public MemoryUserStateStorage(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<UserState> GetStateAsync(long chatId)
    {
        if (_cache.TryGetValue(chatId, out UserState userState))
            return Task.FromResult(userState);

        return Task.FromResult(UserState.None);
    }

    public Task SetStateAsync(long chatId, UserState state)
    {
        _cache.Set(chatId, state, CacheDuration);
        return Task.CompletedTask;
    }
}
