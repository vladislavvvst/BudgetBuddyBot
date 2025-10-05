using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Common;

/// <summary>
/// Главный роутер бота на основе сцен.
/// Находит правильную сцену и вызывает у нее нужный метод.
/// </summary>
internal static class SceneRouter
{
    private static readonly Dictionary<string, Func<UpdateContext, CancellationToken, Task>?> TopMenu =
        new(StringComparer.Ordinal)
        {
            [UiStrings.CallbackData.AddExpense] = static (ctx, ct) => SceneRegistry.Resolve(UserState.ExpenseAddPickCategory).EnterAsync(ctx, ct),
            [UiStrings.CallbackData.Categories] = static (ctx, ct) => SceneRegistry.Resolve(UserState.CategoryMenu).EnterAsync(ctx, ct),
            [UiStrings.CallbackData.Stats] = static (ctx, ct) => SceneRegistry.Resolve(UserState.StatisticsWaitPeriod).EnterAsync(ctx, ct),
            [UiStrings.CallbackData.LastExpenses] = static (ctx, ct) => ShowExpensesListAsync(ctx, ct)
        };

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
        UpdateKind kind = GetKind(update);

        if (kind == UpdateKind.Message && update.Message is { Text: { } txt } && txt.StartsWith('/'))
        {
            string cmd = txt.Split(' ', 2)[0];
            if (Commands.TryGetValue(cmd, out Func<UpdateContext, CancellationToken, Task>? cmdHandler))
            {
                if (cmdHandler != null)
                    await cmdHandler(context, ct);

                return;
            }
        }

        if (kind == UpdateKind.CallbackQuery && update.CallbackQuery is { Data: not null })
        {
            string data = update.CallbackQuery.Data;

            if (string.Equals(data, UiStrings.CallbackData.NavBack, StringComparison.Ordinal))
            {
                await context.Bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);

                long chatId0 = update.CallbackQuery.Message!.Chat.Id;
                UserState? prev = BackStackService.Pop(chatId0);

                if (prev.HasValue)
                    await SceneRegistry.Resolve(prev.Value).EnterAsync(context, ct);
                else
                    await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);

                return;
            }

            if (TopMenu.TryGetValue(data, out Func<UpdateContext, CancellationToken, Task>? handler))
            {
                await context.Bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);

                if (handler != null)
                    await handler(context, ct);

                return;
            }
        }

        long chatId = Utils.ChatId(context);
        UserState state = await context.StateCache.GetStateAsync(chatId);
        IScene scene = SceneRegistry.Resolve(state);

        switch (kind)
        {
            case UpdateKind.CallbackQuery:
                await scene.OnCallbackAsync(context, ct);
                return;
            case UpdateKind.Message:
                await scene.OnMessageAsync(context, ct);
                return;
            case UpdateKind.Unknown:
            case UpdateKind.Command:
            default:
                await scene.EnterAsync(context, ct);
                return;
        }
    }

    private static UpdateKind GetKind(Update update)
    {
        if (update.CallbackQuery != null)
            return UpdateKind.CallbackQuery;

        return update.Message != null ? UpdateKind.Message : UpdateKind.Unknown;
    }

    private static async Task GoMainMenuAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        BackStackService.Clear(chatId);
        await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);
    }

    private static async Task ShowAboutAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Prompts.AboutBot,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: ct);

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Prompts.ChooseAction,
            replyMarkup: UiKeyboards.BuildMainMenuInline,
            cancellationToken: ct);
    }

    private static async Task GlobalCancelAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.StateCache.RemoveCategoryIdAsync(chatId);
        BackStackService.Clear(chatId);
        await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);
    }

    /// <summary>
    /// Показывает последние 10 расходов пользователя
    /// </summary>
    private static async Task ShowExpensesListAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        GetExpensesResponse response = await context.Tracker.GetExpensesAsync(new(chatId, 1, 10), ct);

        if (response.Items.Count == 0)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Info.NoExpenses, cancellationToken: ct);
            return;
        }

        IReadOnlyList<CategoryDto> categories = await Utils.GetUserCategories(context, ct);
        Dictionary<long, string> byId = categories.ToDictionary(x => x.Id, x => x.Name);

        IEnumerable<string> lines = response.Items.Select(i =>
        {
            string categoryName = byId.TryGetValue(i.CategoryId, out string? name)
                ? name
                : $"{UiStrings.Errors.ErrorNameCategory}{i.CategoryId}";
            return UiStrings.ExpenseLine(i.AddedAtUtc, categoryName, i.Amount, i.Comment);
        });

        string text = $"{UiStrings.Info.LastExpensesHeader}\n{string.Join("\n", lines)}";
        await context.Bot.SendMessage(chatId, text, cancellationToken: ct);
    }
}
