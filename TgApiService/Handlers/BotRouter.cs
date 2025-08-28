using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Entities;
using TgApiService.Services;

namespace TgApiService.Handlers;

internal enum UpdateKind { Message, CallbackQuery, Command }

internal static class BotRouter
{
    // Куда роутим: (состояние, тип апдейта) -> обработчик
    private static readonly Dictionary<(UserState, UpdateKind), Func<HandlerContext, CancellationToken, Task>> _byState
        = new()
        {
            { (UserState.ExpenseAdd_WaitExpense,    UpdateKind.Message),        Handlers.Expense_WaitExpenseAsync           },
            { (UserState.CategoryMenu,              UpdateKind.CallbackQuery),  Handlers.Category_MenuCallbackAsync         },
            { (UserState.CategoryDelete_WaitChoice, UpdateKind.CallbackQuery),  Handlers.Category_DeleteChoiceCallbackAsync },
            { (UserState.CategoryAdd_WaitName,      UpdateKind.Message),        Handlers.Category_AddNameAsync              },
        };

    // Команды/кнопки, доступные из любого состояния
    private static readonly Dictionary<string, Func<HandlerContext, CancellationToken, Task>> _commands
        = new(StringComparer.OrdinalIgnoreCase)
        {
            { BotTexts.Menu,   Handlers.MainMenuAsync   },
            { BotTexts.Start,  Handlers.StartAsync      },
            { BotTexts.About,  Handlers.AboutAsync      },
            { BotTexts.Cancel, Handlers.CancelAsync     },
        };

    // Топ-уровень инлайн-меню (BotMenuMap)
    private static readonly Dictionary<BotMenuAction, Func<HandlerContext, CancellationToken, Task>> _topMenu
        = new()
        {
            { BotMenuAction.AddExpense,         Handlers.Expense_StartAsync     },
            { BotMenuAction.ShowStats,          Handlers.StatsPlaceholderAsync  },
            { BotMenuAction.ShowCategories,     Handlers.Category_MenuAsync     },
            { BotMenuAction.ShowAllExpenses,    Handlers.Expenses_ListAsync     },
        };

    public static UpdateKind GetKind(in Update update) =>
        update.CallbackQuery is not null ? UpdateKind.CallbackQuery :
        update.Message?.Text?.StartsWith('/') == true ? UpdateKind.Command : UpdateKind.Message;

    public static async Task RouteAsync(HandlerContext context, CancellationToken ct)
    {
        Update update = context.Update;
        UpdateKind kind = GetKind(update);

        // Команды (из любого состояния)
        if (kind is UpdateKind.Command || kind is UpdateKind.Message)
        {
            string text = update.Message?.Text?.Split(' ', 2)[0] ?? "";
            if (!string.IsNullOrEmpty(text) && _commands.TryGetValue(text, out var cmd))
            {
                await cmd(context, ct);
                return;
            }
        }

        // Топовое inline-меню (кнопки)
        if (kind is UpdateKind.CallbackQuery && update.CallbackQuery?.Data is string data &&
            BotMenuMap.TryParseActionKey(data, out var topAction) &&
            _topMenu.TryGetValue(topAction, out var topHandler))
        {
            await context.Bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
            await topHandler(context, ct);
            return;
        }

        // Внутренние шаги по состояниям
        UserState userState = await context.StateStorage.GetStateAsync(ChatId(context.Update));
        if (_byState.TryGetValue((userState, kind), out var handler))
        {
            await handler(context, ct);
            return;
        }

        await Handlers.MainMenuAsync(context, ct);
    }

    private static long ChatId(Update update) =>
        update.Message?.Chat.Id ?? update.CallbackQuery!.Message!.Chat.Id;
}
