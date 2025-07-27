using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Entities;
using TgApiService.Options;

namespace TgApiService.Services.Implementations;

internal enum UserState
{
    None, WaitAddExpense
};

internal class UpdateHandlerService : IUpdateHandler
{
    private readonly ILogger<UpdateHandlerService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IOptions<TelegramOptions> _options;

    private UserState _userState = UserState.None;

    public UpdateHandlerService(ILogger<UpdateHandlerService> logger, ITelegramBotClient botClient, IOptions<TelegramOptions> options)
    {
        _logger = logger;
        _botClient = botClient;
        _options = options;
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogError("HandleError: {Exception}", exception);

        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await (update switch
        {
            { Message: { } message } => OnMessage(message),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        _logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        await MessageHandlerAsync(msg);
    }

    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        await _botClient.AnswerCallbackQuery(callbackQuery.Id);

        if (!BotMenuMap.TryParseActionKey(callbackQuery.Data, out var action))
        {
            _logger.LogWarning(
                "Unknown callback data received: '{Data}', CallbackQueryId={CallbackQueryId}, FromUserId={UserId}",
                callbackQuery.Data, callbackQuery.Id, callbackQuery.From?.Id);
            return;
        }

        if (callbackQuery.Message is null)
        {
            _logger.LogWarning(
                "CallbackQuery without message: Data='{Data}', CallbackQueryId={CallbackQueryId}, FromUserId={UserId}",
                callbackQuery.Data, callbackQuery.Id, callbackQuery.From?.Id);
            return;
        }

        if (action != BotMenuAction.AddExpense)
            _userState = UserState.None;

        Chat chat = callbackQuery.Message.Chat;

        switch (action)
        {
            case BotMenuAction.AddExpense:
                _userState = UserState.WaitAddExpense;
                await StartExpenseFlow(chat);
                break;
            case BotMenuAction.ShowStats:
                await _botClient.SendMessage(chat, "Здесь будет статистика");
                break;
            case BotMenuAction.ShowCategories:
                await _botClient.SendMessage(chat, "Список категорий: ...");
                break;
            case BotMenuAction.ShowAllExpenses:
                await _botClient.SendMessage(chat, "Вот ваши траты: ...");
                break;
            case BotMenuAction.Settings:
                await _botClient.SendMessage(chat, "Настройки бота ");
                break;
            default:
                await _botClient.SendMessage(chat, "Неизвестная команда");
                break;
        }
    }

    private async Task<Message> MessageHandlerAsync(Message msg)
    {
        if (msg.Chat.Id != _options.Value.UserId)
            return await _botClient.SendMessage(msg.Chat, "⛔️ Доступ запрещён! Этот бот только для владельца");

        if (msg.Chat.Type != ChatType.Private)
            return await _botClient.SendMessage(msg.Chat, "⛔️ Доступ запрещён! Бот работает только в личных сообщениях");

        if (msg.Text?.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture) == "/menu")
            return await ShowMainMenu(msg.Chat);

        return _userState switch
        {
            UserState.WaitAddExpense => await ExpenseAddHandlerAsync(msg),
            _ => await _botClient.SendMessage(msg.Chat, msg.Text ?? string.Empty)
        };
    }

    private Task<Message> ShowMainMenu(Chat chat)
    {
        return _botClient.SendMessage(
            chat, "Выберите действие:", parseMode: ParseMode.Html, replyMarkup: BuildMainMenuInline);
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private async Task<Message> StartExpenseFlow(Chat chat)
    {
        return await _botClient.SendMessage
        (
            chat,
            text: "Введите категорию, сумму и комментарий (опционально), например:\n<b>Топливо 1500 Лукойл</b>\n" +
                "Для отмены напишите: <b>/cancel</b>",
            parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove()
        );
    }

    private async Task<Message> ExpenseAddHandlerAsync(Message msg)
    {
        if (msg.Text?.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture) == "/cancel")
        {
            _userState = UserState.None;
            return await _botClient.SendMessage(msg.Chat, "⛔️ Действие отменено");
        }

        string? input = msg.Text?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return await _botClient.SendMessage(msg.Chat, "Пустой ввод. Попробуйте ещё раз\nДля отмены напишите: /cancel");

        string[] parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return await _botClient.SendMessage(msg.Chat, "Формат: <b>Категория Сумма [Комментарий]</b>\nПример: " +
                "<b>Топливо 1500 Лукойл</b>", parseMode: ParseMode.Html);

        // Категория
        string categoryText = parts[0];
        if (!ExpenseCategoryParser.TryParse(categoryText, out var category))
            return await _botClient.SendMessage(msg.Chat, $"Такой категории нет. Доступные:\n" +
                $"<b>{string.Join(", ", ExpenseCategoryParser.AllDisplayNames())}</b>\nДля отмены напишите: /cancel",
                parseMode: ParseMode.Html);

        // Сумма
        if (!decimal.TryParse(parts[1].Replace(',', '.'), out var amount) || amount <= 0)
            return await _botClient.SendMessage(msg.Chat, "Некорректная сумма. Введите положительное число\n" +
                "Для отмены напишите: /cancel", parseMode: ParseMode.Html);

        // Комментарий
        string? comment = parts.Length > 2 ? parts[2] : null;

        // todo: тут формируем и отправляем DTO, например: SendExpense(category, amount, comment);

        _userState = UserState.None;

        return await _botClient.SendMessage
        (
            msg.Chat,
            $"Трата <b>{amount}₽</b> в категорию <b>{categoryText}</b> добавлена!" +
                $"{(comment != null ? $"\nКомментарий: {comment}" : "")}",
            parseMode: ParseMode.Html
        );
    }

    private InlineKeyboardMarkup BuildMainMenuInline { get; } = new
    ([
        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.AddExpense),
            BotMenuMap.GetActionKey(BotMenuAction.AddExpense))],

        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.ShowStats),
            BotMenuMap.GetActionKey(BotMenuAction.ShowStats))],

        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.ShowCategories),
            BotMenuMap.GetActionKey(BotMenuAction.ShowCategories))],

        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.ShowAllExpenses),
            BotMenuMap.GetActionKey(BotMenuAction.ShowAllExpenses))],

        [InlineKeyboardButton.WithCallbackData(BotMenuMap.GetText(BotMenuAction.Settings),
            BotMenuMap.GetActionKey(BotMenuAction.Settings))]
    ]);
}
