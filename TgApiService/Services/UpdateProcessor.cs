using MassTransit;
using Microsoft.Extensions.Options;
using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgApiService.Entities;
using TgApiService.Options;

namespace TgApiService.Services;

internal class UpdateProcessor
{
    private readonly ILogger<UpdateProcessor> _logger;
    private readonly IOptions<TelegramOptions> _tgOptions;
    private readonly IUserStateStorage _stateStorage;
    private readonly ISendEndpointProvider _sendProvider;
    private readonly IRequestClient<GetExpenses> _getExpensesClient;

    public UpdateProcessor
    (
        ILogger<UpdateProcessor> logger, IOptions<TelegramOptions> tgOptions, ISendEndpointProvider sendProvider,
        IRequestClient<GetExpenses> getExpensesClient, IUserStateStorage stateStorage
    )
    {
        _logger = logger;
        _tgOptions = tgOptions;
        _stateStorage = stateStorage;
        _sendProvider = sendProvider;
        _getExpensesClient = getExpensesClient;
    }

    public async Task HandleErrorAsync
    (
        ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken
    )
    {
        _logger.LogError("HandleError: {Exception}", exception);

        if (exception is Telegram.Bot.Exceptions.RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await (update switch
        {
            { Message: { } message } => OnMessage(botClient, message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(botClient, callbackQuery, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        await MessageHandlerAsync(botClient, msg, cancellationToken);
    }

    private async Task OnCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

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
                await StartExpenseFlow(botClient, chatId);
                break;
            case BotMenuAction.ShowStats:
                await botClient.SendMessage(chatId, BotTexts.StatsPlaceholder, cancellationToken: cancellationToken);
                break;
            case BotMenuAction.ShowCategories:
                await ShowCategoriesHandlerAsync(botClient, chatId, cancellationToken);
                break;
            case BotMenuAction.ShowAllExpenses:
                await GetAllExpensesHandlerAsync(botClient, chatId, cancellationToken);
                break;
            case BotMenuAction.Settings:
                await botClient.SendMessage(chatId, BotTexts.SettingsPlaceholder, cancellationToken: cancellationToken);
                break;
            default:
                await botClient.SendMessage(chatId, BotTexts.UnknownCmd, cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task MessageHandlerAsync(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken)
    {
        if (msg.Chat.Id != _tgOptions.Value.UserId)
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.AccessDeniedOwner, cancellationToken: cancellationToken);
            return;
        }

        if (msg.Chat.Type != ChatType.Private)
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.AccessDeniedPrivate, cancellationToken: cancellationToken);
            return;
        }

        if (msg.Text == BotTexts.Menu)
        {
            await ShowMainMenu(botClient, msg.Chat.Id);
            return;
        }

        if (msg.Text == BotTexts.Start)
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.StartText, parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            await ShowMainMenu(botClient, msg.Chat.Id);
            return;
        }

        if (msg.Text == BotTexts.About)
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.AboutBot, parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        UserState state = await _stateStorage.GetStateAsync(msg.Chat.Id);
        if (state == UserState.WaitAddExpense)
            await AddExpenseHandlerAsync(botClient, msg, cancellationToken);

        return;
    }

    private static async Task ShowMainMenu(ITelegramBotClient botClient, long chatId)
    {
        await botClient.SendMessage(
            chatId, BotTexts.ChooseAction, parseMode: ParseMode.Html, replyMarkup: BuildMainMenuInline);
    }

    private static async Task ShowCategoriesHandlerAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId,
            BotTexts.CategoriesList(ExpenseCategories.All()),
            parseMode: ParseMode.Html, cancellationToken: cancellationToken);
    }

    private async Task GetAllExpensesHandlerAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        const int pageSize = 10;
        Response<ExpensesPage> response = await _getExpensesClient.GetResponse<ExpensesPage>(new(1, pageSize), cancellationToken);

        if (!response.Message.Items.Any())
        {
            await botClient.SendMessage(chatId, BotTexts.NoExpenses, cancellationToken: cancellationToken);
            return;
        }

        IEnumerable<string> lines = response.Message.Items.Select(
            i => BotTexts.ExpenseLine(i.AddDateUtc, i.Category, i.Amount, i.Comment));
        string text = $"{BotTexts.LastExpensesHeader}\n{string.Join("\n", lines)}";

        await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private async Task StartExpenseFlow(ITelegramBotClient botClient, long chatId)
    {
        await _stateStorage.SetStateAsync(chatId, UserState.WaitAddExpense);
        await botClient.SendMessage
        (
            chatId,
            BotTexts.StartExpensePrompt,
            parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove()
        );
    }

    private async Task AddExpenseHandlerAsync(ITelegramBotClient botClient, Message msg, CancellationToken cancellationToken)
    {
        if (msg.Text?.Trim().ToLower(System.Globalization.CultureInfo.CurrentCulture) == "/cancel")
        {
            await _stateStorage.SetStateAsync(msg.Chat.Id, UserState.None);
            await botClient.SendMessage(msg.Chat.Id, BotTexts.Cancelled, cancellationToken: cancellationToken);
            return;
        }

        string? input = msg.Text?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.EmptyInput, cancellationToken: cancellationToken);
            return;
        }

        string[] parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.BadFormat, parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        // Категория
        string categoryText = parts[0];
        if (!ExpenseCategories.TryNormalize(categoryText, out string? category))
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.CategoryNotFound(ExpenseCategories.All()),
                parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            return;
        }

        // Сумма
        if (!decimal.TryParse(parts[1], System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            await botClient.SendMessage(msg.Chat.Id, BotTexts.BadAmount, parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        // Комментарий
        string? comment = parts.Length > 2 ? parts[2] : null;

        // Публикация сообщения в очередь
        await _sendProvider.Send<AddExpense>(new(category, amount, comment), cancellationToken);

        await _stateStorage.SetStateAsync(msg.Chat.Id, UserState.None);
        await botClient.SendMessage
        (
            msg.Chat.Id,
            BotTexts.ExpenseAdded(amount, category, comment),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }

    private static InlineKeyboardMarkup BuildMainMenuInline { get; } = new
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
