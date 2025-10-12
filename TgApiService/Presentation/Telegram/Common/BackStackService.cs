using System.Collections.Concurrent;
using TgApiService.Application.Cache;

namespace TgApiService.Presentation.Telegram.Common;

/// <summary>
/// Позволяет сценам запоминать, с какого экрана пользователь пришел.
/// Хранит в памяти только цепочку состояний UserState.
/// </summary>
internal static class BackStackService
{
    private static readonly ConcurrentDictionary<long, Stack<UserState>?> Stacks = new();

    public static void Push(long chatId, UserState currentState)
    {
        Stack<UserState>? stack = Stacks.GetOrAdd(chatId, static _ => new Stack<UserState>());
        stack?.Push(currentState);
    }

    public static UserState? Pop(long chatId)
    {
        if (Stacks.TryGetValue(chatId, out Stack<UserState>? stack) && stack is { Count: > 0 })
            return stack.Pop();

        return null;
    }

    public static void Clear(long chatId)
    {
        Stacks.TryRemove(chatId, out Stack<UserState>? _);
    }
}
