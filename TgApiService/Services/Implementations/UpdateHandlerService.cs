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

internal class UpdateHandlerService : IUpdateHandler
{
    private readonly ILogger<UpdateHandlerService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IOptions<TelegramOptions> _options;

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
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        if (msg.Chat.Id != _options.Value.UserId)
        {
            await _botClient.SendMessage(msg.Chat, "⛔️ Доступ запрещён! Этот бот только для владельца");
            return;
        }

        if (msg.Chat.Type != ChatType.Private)
        {
            await _botClient.SendMessage(msg.Chat, "⛔️ Доступ запрещён! Бот работает только в личных сообщениях");
            return;
        }

        _logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        await MessageHandlerAsync(msg);
    }

    private async Task<Message> MessageHandlerAsync(Message msg)
    {
        ReplyKeyboardMarkup keyboard = new
        ([
            [BotMenuTexts.AddExpense, BotMenuTexts.Stats],
            [BotMenuTexts.Categories, BotMenuTexts.AllExpenses ],
            [BotMenuTexts.Settings, BotMenuTexts.HideMenu]
        ])
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        return msg.Text switch
        {
            BotMenuTexts.AddExpense => await _botClient.SendMessage(msg.Chat, text: "Введите сумму и категорию, например: <b>150 еда</b>.", parseMode: ParseMode.Html),
            BotMenuTexts.Stats => await _botClient.SendMessage(msg.Chat, text: "Здесь будет статистика 📊"),
            BotMenuTexts.Categories => await _botClient.SendMessage(msg.Chat, text: "Список категорий: ..."),
            BotMenuTexts.AllExpenses => await _botClient.SendMessage(msg.Chat, text: "Вот ваши траты: ..."),
            BotMenuTexts.Settings => await _botClient.SendMessage(msg.Chat, text: "Настройки бота ⚙️"),
            BotMenuTexts.HideMenu => await _botClient.SendMessage(msg.Chat, text: "Меню скрыто", replyMarkup: new ReplyKeyboardRemove()),
            _ => await _botClient.SendMessage(msg.Chat, text: "Выберите действие:", parseMode: ParseMode.Html, replyMarkup: keyboard)
        };
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
