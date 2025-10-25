using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Features.Categories;
using TgApiService.Presentation.Telegram.Features.Expenses;
using TgApiService.Presentation.Telegram.Features.MainMenu;
using TgApiService.Presentation.Telegram.Features.Statistics;

namespace TgApiService.Presentation.Telegram.Common;

/// <summary>
/// Глобальный реестр всех сцен.
/// При старте приложения регистрируем все сцены один раз.
/// Потом SceneRouter по UserState пользователя достает из этого словаря нужный экземпляр.
/// </summary>
internal static class SceneRegistry
{
    private static readonly Dictionary<UserState, IScene> ByState = [];

    public static IScene GetScene(UserState state) => Resolve(state);

    public static async Task NavigateBackAsync(UpdateContext context, UserState fallback, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        UserState state = BackStackService.Pop(chatId) ?? fallback;
        await context.StateCache.SetStateAsync(chatId, state);
        await Resolve(state).EnterAsync(context, ct);
    }

    public static async Task NavigateForwardAsync(UpdateContext context, UserState nextState, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        UserState current = await context.StateCache.GetStateAsync(chatId);
        BackStackService.Push(chatId, current);
        await context.StateCache.SetStateAsync(chatId, nextState);
        await Resolve(nextState).EnterAsync(context, ct);
    }

    private static void Register(IScene scene) => ByState[scene.State] = scene;

    private static IScene Resolve(UserState state) => ByState.TryGetValue(state, out IScene? scene) ? scene : ByState[UserState.MainMenu];

    public static void Bootstrap()
    {
        Register(new MainMenuScene());
        Register(new ExpensePickCategoryScene());
        Register(new ExpenseAmountCommentScene());
        Register(new ExpenseShowLastScene());
        Register(new CategoryMenuScene());
        Register(new CategoryDeleteScene());
        Register(new CategoryAddNameScene());
        Register(new StatsPeriodScene());
        Register(new StatsMetricScene());
        Register(new StatsFullWeekScene());
        Register(new StatsMetricTotalAmountScene());
    }
}
