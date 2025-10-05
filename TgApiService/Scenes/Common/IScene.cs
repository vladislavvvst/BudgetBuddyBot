using TgApiService.Cache;
using TgApiService.Common;

namespace TgApiService.Scenes.Common;

/// <summary>
/// Интерфейс для всех сцен бота.
/// Сцена представляет один экран/шаг в пользовательском сценарии (например:
/// главное меню, выбор категории, ввод суммы, статистика и т.д.).
/// </summary>
internal interface IScene
{
    /// <summary>
    /// К какому UserState привязана сцена.
    /// </summary>
    UserState State { get; }
    /// <summary>
    /// Вызывается при входе в сцену.
    /// </summary>
    Task EnterAsync(UpdateContext context, CancellationToken ct);
    /// <summary>
    /// Вызывается, когда приходит сообщение от пользователя.
    /// </summary>
    Task OnMessageAsync(UpdateContext context, CancellationToken ct);
    /// <summary>
    /// Вызывается, когда приходит callback-кнопка из inline-клавиатуры.
    /// </summary>
    Task OnCallbackAsync(UpdateContext context, CancellationToken ct);
    /// <summary>
    /// Возврат к предыдущей сцене.
    /// </summary>
    Task OnBackAsync(UpdateContext context, CancellationToken ct);
}
