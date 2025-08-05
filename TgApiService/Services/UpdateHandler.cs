using Microsoft.Extensions.Options;
using RabbitMqMessaging;
using RabbitMqMessaging.Publisher;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Entities;
using TgApiService.Options;

namespace TgApiService.Services;

internal class UpdateHandler : IUpdateHandler
{
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IOptions<TelegramOptions> _tgOptions;
    private readonly IOptions<RabbitMqOptions> _mqOptions;
    private readonly IUserStateStorage _stateStorage;
    private readonly IMessagePublisher _publisher;

    public UpdateHandler
    (
        ILogger<UpdateHandler> logger, ITelegramBotClient botClient, IOptions<TelegramOptions> tgOptions,
        IOptions<RabbitMqOptions> mqOptions, IUserStateStorage stateStorage, IMessagePublisher publisher
    )
    {
        _logger = logger;
        _botClient = botClient;
        _tgOptions = tgOptions;
        _mqOptions = mqOptions;
        _stateStorage = stateStorage;
        _publisher = publisher;
    }

    public async Task HandleErrorAsync
    (
        ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken
    )
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

        long chatId = callbackQuery.Message.Chat.Id;

        // Сброс состояния при нажатии на другую кнопку кроме добавления траты
        if (action != BotMenuAction.AddExpense)
            await _stateStorage.SetStateAsync(chatId, UserState.None);

        switch (action)
        {
            case BotMenuAction.AddExpense:
                await StartExpenseFlow(chatId);
                break;
            case BotMenuAction.ShowStats:
                await _botClient.SendMessage(chatId, "Здесь будет статистика");
                break;
            case BotMenuAction.ShowCategories:
                await ShowCategoriesHandlerAsync(chatId);
                break;
            case BotMenuAction.ShowAllExpenses:
                await _botClient.SendMessage(chatId, "Вот ваши траты: ...");
                break;
            case BotMenuAction.Settings:
                await _botClient.SendMessage(chatId, "Настройки бота ");
                break;
            default:
                await _botClient.SendMessage(chatId, "Неизвестная команда");
                break;
        }
    }

    private async Task MessageHandlerAsync(Message msg)
    {
        if (msg.Chat.Id != _tgOptions.Value.UserId)
        {
            await _botClient.SendMessage(msg.Chat.Id, "⛔️ Доступ запрещён! Этот бот только для владельца");
            return;
        }

        if (msg.Chat.Type != ChatType.Private)
        {
            await _botClient.SendMessage(msg.Chat.Id, "⛔️ Доступ запрещён! Бот работает только в личных сообщениях");
            return;
        }

        if (msg.Text == "/menu")
        {
            await ShowMainMenu(msg.Chat.Id);
            return;
        }

        UserState state = await _stateStorage.GetStateAsync(msg.Chat.Id);
        if (state == UserState.WaitAddExpense)
            await AddExpenseHandlerAsync(msg);

        return;
    }

    private async Task ShowMainMenu(long chatId)
    {
        await _botClient.SendMessage(
            chatId, "Выберите действие:", parseMode: ParseMode.Html, replyMarkup: BuildMainMenuInline);
    }

    private async Task ShowCategoriesHandlerAsync(long chatId)
    {
        await _botClient.SendMessage(
            chatId,
            text: $"Список категорий: <b>{string.Join(", ", ExpenseCategoryParser.AllDisplayNames())}</b>",
            parseMode: ParseMode.Html);
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private async Task StartExpenseFlow(long chatId)
    {
        await _stateStorage.SetStateAsync(chatId, UserState.WaitAddExpense);
        await _botClient.SendMessage
        (
            chatId: chatId,
            text: "Введите категорию, сумму и комментарий (опционально), например:\n<b>Топливо 1500 Лукойл</b>\n" +
                "Для отмены напишите: <b>/cancel</b>",
            parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove()
        );
    }

    private async Task AddExpenseHandlerAsync(Message msg)
    {
        if (msg.Text?.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture) == "/cancel")
        {
            await _stateStorage.SetStateAsync(msg.Chat.Id, UserState.None);
            await _botClient.SendMessage(msg.Chat.Id, "⛔️ Действие отменено");
            return;
        }

        string? input = msg.Text?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            await _botClient.SendMessage(msg.Chat.Id, "Пустой ввод. Попробуйте ещё раз\nДля отмены напишите: /cancel");
            return;
        }

        string[] parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await _botClient.SendMessage(msg.Chat.Id, "Формат: <b>Категория Сумма [Комментарий]</b>\nПример: " +
                "<b>Топливо 1500 Лукойл</b>", parseMode: ParseMode.Html);
            return;
        }

        // Категория
        string categoryText = parts[0];
        if (!ExpenseCategoryParser.TryParse(categoryText, out var category))
        {
            await _botClient.SendMessage(msg.Chat.Id, $"Такой категории нет. Доступные:\n" +
                $"<b>{string.Join(", ", ExpenseCategoryParser.AllDisplayNames())}</b>\nДля отмены напишите: /cancel",
                parseMode: ParseMode.Html);
            return;
        }

        // Сумма
        if (!decimal.TryParse(parts[1].Replace(',', '.'), out var amount) || amount <= 0)
        {
            await _botClient.SendMessage(msg.Chat.Id, "Некорректная сумма. Введите положительное число\n" +
                "Для отмены напишите: /cancel", parseMode: ParseMode.Html);
            return;
        }

        // Комментарий
        string? comment = parts.Length > 2 ? parts[2] : null;

        string mqMessage = $"{category} {amount} {comment}";
        await _publisher.PublishAsync(mqMessage, _mqOptions.Value.AddExpenseQueueName);
        //await _mqPublisher.PublishAsync
        //(
        //    message: mqMessage,
        //    messageBus: new(HostName: _mqOptions.Value.HostName, QueueName: _mqOptions.Value.AddExpenseQueueName)
        //);

        await _stateStorage.SetStateAsync(msg.Chat.Id, UserState.None);
        await _botClient.SendMessage
        (
            msg.Chat.Id,
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
