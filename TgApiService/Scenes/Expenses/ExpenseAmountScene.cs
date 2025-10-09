using MassTransit;
using SharedTypes;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Expenses;

/// <summary>
/// Сцена ввода суммы и комментария для новой траты.
/// Попадаем сюда после выбора категории.
/// Проверяем ввод, шлем RPC AddExpenseAsync.
/// </summary>
internal sealed class ExpenseAmountScene : IScene
{
    private const int MaxExpenseAmountNameLength = 64;
    public UserState State => UserState.ExpenseAddWaitAmountComment;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Push(chatId, UserState.ExpenseAddPickCategory);

        await context.StateCache.SetStateAsync(chatId, UserState.ExpenseAddWaitAmountComment);
        await context.Bot.SendMessage(chatId, UiStrings.Prompts.StartExpensePrompt, parseMode: ParseMode.Html,
            replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        Message msg = context.Update.Message!;

        if (msg.Type is not MessageType.Text)
        {
            await context.Bot.SendMessage(chatId, $"{UiStrings.Errors.TextExpected}\n{UiStrings.Prompts.StartExpensePrompt}",
                parseMode: ParseMode.Html, replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
            return;
        }

        if (msg.Text is { Length: > MaxExpenseAmountNameLength })
        {
            await context.Bot.SendMessage(chatId, $"{UiStrings.Errors.StringTooLong}\n{UiStrings.Prompts.StartExpensePrompt}",
                parseMode: ParseMode.Html, replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
            return;
        }

        string? categoryIdStr = await context.StateCache.GetCategoryIdAsync(chatId);

        if (string.IsNullOrEmpty(categoryIdStr) ||
            !long.TryParse(categoryIdStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long categoryId))
        {
            await SceneRegistry.Resolve(UserState.ExpenseAddPickCategory).EnterAsync(context, ct);
            return;
        }

        if (!TryParseAmountAndComment(msg.Text ?? string.Empty, out decimal amount, out string? comment))
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.BadFormat, parseMode: ParseMode.Html,
                replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
            return;
        }

        if (amount <= 0m)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.BadAmount, cancellationToken: ct);
            return;
        }

        string categoryName = await ResolveCategoryNameAsync(context, categoryId, ct);

        try
        {
            string requestId = $"{chatId}:{msg.MessageId}";

            AddExpenseRequest request = new(chatId, categoryId, amount, comment, requestId);
            AddExpenseResponse response = await context.Tracker.AddExpenseAsync(request, ct);

            if (response.Success)
            {
                await context.Bot.SendMessage(chatId, UiStrings.ExpenseAdded(amount, categoryName, comment),
                    parseMode: ParseMode.Html, cancellationToken: ct);

                await context.StateCache.RemoveCategoryIdAsync(chatId);

                BackStackService.Pop(chatId);
                await SceneRegistry.Resolve(UserState.MainMenu).EnterAsync(context, ct);

                return;
            }

            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorAddingExpense, cancellationToken: ct);
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "AddExpense fault");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorProcessing, cancellationToken: ct);
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogError("AddExpense timeout");
            await context.Bot.SendMessage(chatId, UiStrings.Errors.ErrorTimeout, cancellationToken: ct);
        }
    }

    public async Task OnCallbackAsync(UpdateContext context, CancellationToken ct)
    {
        CallbackQuery cq = context.Update.CallbackQuery!;
        long chatId = cq.Message!.Chat.Id;
        string data = cq.Data ?? string.Empty;

        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

        if (string.Equals(data, UiStrings.CallbackData.NavBack, StringComparison.Ordinal))
        {
            await OnBackAsync(context, ct);
            return;
        }

        await context.Bot.SendMessage(chatId, UiStrings.Info.PushButton, cancellationToken: ct);
    }

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.StateCache.RemoveCategoryIdAsync(chatId);
        BackStackService.Pop(chatId);
        await SceneRegistry.Resolve(UserState.ExpenseAddPickCategory).EnterAsync(context, ct);
    }

    /// <summary>
    /// Парсит строку в формате: "сумма комментарий".
    /// Сумма обязательно идёт первой, далее произвольный комментарий.
    /// Разрешены разделители дробной части "." или ",".
    /// </summary>
    private static bool TryParseAmountAndComment(string input, out decimal amount, out string? comment)
    {
        amount = 0m;
        comment = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Обрезаем ведущие/хвостовые пробелы
        input = input.Trim();

        // Разделяем только по первому пробелу, чтобы не терять остальные слова комментария
        int spaceIndex = input.IndexOf(' ');
        string numberPart;
        string commentPart = string.Empty;

        if (spaceIndex == -1)
        {
            // Введена только сумма, комментария нет
            numberPart = input;
        }
        else
        {
            numberPart = input[..spaceIndex];
            commentPart = input[(spaceIndex + 1)..].Trim();
        }

        // Нормализуем десятичный разделитель
        numberPart = numberPart.Replace(',', '.');

        if (!decimal.TryParse(numberPart, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
            return false;

        amount = parsed;
        comment = string.IsNullOrWhiteSpace(commentPart) ? null : commentPart;
        return true;
    }

    /// <summary>
    /// Достает имя категории по Id из кеша пользователя. Если не нашли — возвращает "#id".
    /// </summary>
    private static async Task<string> ResolveCategoryNameAsync(UpdateContext context, long categoryId, CancellationToken ct)
    {
        IReadOnlyList<CategoryDto> categories = await Utils.GetUserCategories(context, ct);
        foreach (CategoryDto t in categories)
        {
            if (t.Id == categoryId)
                return t.Name;
        }

        return $"#{categoryId.ToString(CultureInfo.InvariantCulture)}";
    }
}
