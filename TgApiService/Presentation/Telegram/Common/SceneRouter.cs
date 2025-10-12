using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Cache;
using TgApiService.Presentation.Telegram.Features.UI;

namespace TgApiService.Presentation.Telegram.Common;

/// <summary>
/// Главный роутер бота на основе сцен.
/// Находит правильную сцену и вызывает у нее нужный метод.
/// </summary>
internal static class SceneRouter
{
    private static readonly Dictionary<string, Func<UpdateContext, CancellationToken, Task>?> Commands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [UiStrings.Commands.Start]  = GoMainMenuAsync,
            [UiStrings.Commands.Menu]   = GoMainMenuAsync,
            [UiStrings.Commands.About]  = ShowAboutAsync,
            [UiStrings.Commands.Cancel] = GlobalCancelAsync,
        };

    public static async Task RouteAsync(UpdateContext context, CancellationToken ct)
    {
        Update update = context.Update;
        UpdateType updateType = update.Type;

        // Обработка системных команд, начинающихся с '/'
        if (updateType is UpdateType.Message && update.Message is { Text: { } txt } && txt.StartsWith('/'))
        {
            string cmd = txt.Split(' ', 2)[0];
            if (Commands.TryGetValue(cmd, out Func<UpdateContext, CancellationToken, Task>? cmdHandler))
            {
                if (cmdHandler != null)
                    await cmdHandler(context, ct);

                return;
            }
        }

        long chatId = Utils.ChatId(context);
        UserState state = await context.StateCache.GetStateAsync(chatId);
        IScene scene = SceneRegistry.GetScene(state);

        switch (updateType)
        {
            case UpdateType.CallbackQuery:
                await scene.OnCallbackAsync(context, ct);
                return;
            case UpdateType.Message:
                await scene.OnMessageAsync(context, ct);
                return;
            default:
                await scene.EnterAsync(context, ct);
                return;
        }
    }

    private static async Task GoMainMenuAsync(UpdateContext context, CancellationToken ct)
    {
        await SceneRegistry.NavigateForwardAsync(context, UserState.MainMenu, ct);
    }

    private static async Task ShowAboutAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.Bot.SendMessage(chatId, UiStrings.Prompts.AboutBot, parseMode: ParseMode.Html, cancellationToken: ct);
    }

    private static async Task GlobalCancelAsync(UpdateContext context, CancellationToken ct)
    {
        await SceneRegistry.NavigateForwardAsync(context, UserState.MainMenu, ct);
    }
}
