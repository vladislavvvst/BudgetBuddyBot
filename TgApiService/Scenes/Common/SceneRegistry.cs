using TgApiService.Cache;
using TgApiService.Scenes.Categories;
using TgApiService.Scenes.Expenses;
using TgApiService.Scenes.MainMenu;
using TgApiService.Scenes.Stats;

namespace TgApiService.Scenes.Common;

/// <summary>
/// Глобальный реестр всех сцен.
/// При старте приложения регистрируем все сцены один раз.
/// Потом SceneRouter по UserState пользователя достает из этого словаря нужный экземпляр.
/// </summary>
internal static class SceneRegistry
{
    private static readonly Dictionary<UserState, IScene> ByState = [];

    private static void Register(IScene scene)
    {
        ByState[scene.State] = scene;
    }

    public static IScene Resolve(UserState state)
    {
        return ByState.TryGetValue(state, out IScene? scene) ? scene : ByState[UserState.MainMenu];
    }

    public static void Bootstrap()
    {
        Register(new MainMenuScene());
        Register(new ExpensePickCategoryScene());
        Register(new ExpenseAmountScene());
        Register(new CategoryMenuScene());
        Register(new CategoryDeleteScene());
        Register(new CategoryAddNameScene());
        Register(new StatsPeriodScene());
    }
}
