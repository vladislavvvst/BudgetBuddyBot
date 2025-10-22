using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgApiService.Application.Abstractions;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.UI;

namespace TgApiService.Presentation.Telegram.Features.Expenses;

internal sealed class ExpenseShowLastScene : IScene
{
    public UserState State => UserState.ExpenseShowLast;

    public async Task EnterAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        GetExpensesResponse response = await context.Tracker.GetExpensesAsync(new(chatId, 1, 10), ct);

        if (response.Items.Count is 0)
        {
            await context.Bot.SendMessage(chatId, UiStrings.Info.NoExpenses,
                parseMode: ParseMode.Html, replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
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

        string text = $"{UiStrings.Info.LastExpensesHeader}\n\n{string.Join("\n\n", lines)}";
        await context.Bot.SendMessage(chatId, text, parseMode: ParseMode.Html, replyMarkup: UiKeyboards.BackOnlyKb, cancellationToken: ct);
    }

    public async Task OnMessageAsync(UpdateContext context, CancellationToken ct)
    {
        long chatId = Utils.ChatId(context);
        await context.Bot.SendMessage(chatId, UiStrings.Info.PushButton, cancellationToken: ct);
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

    public async Task OnBackAsync(UpdateContext context, CancellationToken ct) =>
        await SceneRegistry.NavigateBackAsync(context, UserState.MainMenu, ct);
}
