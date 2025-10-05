using MassTransit;
using SharedTypes;
using System.Globalization;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgApiService.Cache;
using TgApiService.Common;
using TgApiService.Scenes.Common;
using TgApiService.UI;

namespace TgApiService.Scenes.Expenses;

/// <summary>
/// Сцена ввода суммы и комментария для новой траты.
/// Попадаем сюда после выбора категории.
/// Ждем текст вида "1500 кофе" или "кофе 1500".
/// Проверяем ввод, шлем RPC AddExpenseAsync.
/// </summary>
internal sealed class ExpenseAmountScene : IScene
{
    public UserState State => UserState.ExpenseAddWaitAmountComment;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);

        BackStackService.Push(chatId, UserState.ExpenseAddPickCategory);
        await context.StateCache.SetStateAsync(chatId, UserState.ExpenseAddWaitAmountComment);

        await context.Bot.SendMessage(
            chatId,
            UiStrings.Prompts.StartExpensePrompt,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        Message msg = context.Update.Message!;
        string text = msg.Text ?? string.Empty;

        if (string.Equals(text, UiStrings.Commands.Cancel, StringComparison.Ordinal))
        {
            await OnBackAsync(context, ct);
            return;
        }

        string? categoryIdStr = await context.StateCache.GetCategoryIdAsync(chatId);
        if (string.IsNullOrEmpty(categoryIdStr) ||
            !long.TryParse(categoryIdStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long categoryId))
        {
            await SceneRegistry.Resolve(UserState.ExpenseAddPickCategory).EnterAsync(context, ct);
            return;
        }

        if (!TryParseAmountAndComment(text, out decimal amount, out string? comment))
        {
            await context.Bot.SendMessage(chatId, UiStrings.Errors.BadFormat, cancellationToken: ct);
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
            AddExpenseRequest request = new(
                chatId,
                categoryId,
                amount,
                comment,
                requestId);

            AddExpenseResponse response = await context.Tracker.AddExpenseAsync(request, ct);

            if (response.Success)
            {
                await context.Bot.SendMessage(
                    chatId,
                    UiStrings.ExpenseAdded(amount, categoryName, comment),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    cancellationToken: ct);

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
        CallbackQuery? cq = context.Update.CallbackQuery;
        if (cq == null)
            return;

        string data = cq.Data ?? string.Empty;
        if (string.Equals(data, UiStrings.CallbackData.NavBack, StringComparison.Ordinal))
        {
            await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
            await OnBackAsync(context, ct);
            return;
        }

        long chatId = cq.Message!.Chat.Id;
        await context.Bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
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
    /// Пытается извлечь сумму и комментарий из произвольного текста.
    /// Поддерживает как «1500 кофе», так и «кофе 1500». Разделители: пробелы, запятая/точка для дробной части.
    /// Берем ПЕРВОЕ найденное число как сумму (0.01..).
    /// </summary>
    private static bool TryParseAmountAndComment(string input, out decimal amount, out string? comment)
    {
        amount = 0m;
        comment = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        Match m = Regex.Match(input, @"(?<!\d)(\d+(?:[.,]\d{1,2})?)(?!\d)");
        if (!m.Success)
            return false;

        string numberRaw = m.Groups[1].Value.Replace(',', '.');
        if (!decimal.TryParse(numberRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
            return false;

        amount = parsed;

        string before = input[..m.Index].Trim();
        string after = input[(m.Index + m.Length)..].Trim();
        string combined = string.Join(" ", new[] { before, after }.Where(s => !string.IsNullOrWhiteSpace(s)));
        comment = string.IsNullOrWhiteSpace(combined) ? null : combined;

        return true;
    }

    /// <summary>
    /// Достает имя категории по Id из кеша пользователя; если не нашли — возвращает "#id".
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
