using System.Globalization;
using System.Net;

namespace TgApiService.Entities;

internal static class BotTexts
{
    public const string Menu = "/menu";
    public const string Start = "/start";
    public const string About = "/about";
    public const string StartText = "Привет! Я помогу вести расходы :)";
    public const string AccessDeniedOwner = "⛔️ Доступ запрещён! Этот бот только для владельца";
    public const string AccessDeniedPrivate = "⛔️ Доступ запрещён! Бот работает только в личных сообщениях";
    public const string Cancelled = "⛔️ Действие отменено";
    public const string EmptyInput = "Пустой ввод. Попробуйте ещё раз\nДля отмены напишите: /cancel";
    public const string BadFormat = "Формат: <b>Категория Сумма [Комментарий]</b>\nПример: <b>Топливо 1500 Лукойл</b>";
    public const string BadAmount = "Некорректная сумма. Введите положительное число\nДля отмены напишите: /cancel";
    public const string UnknownCmd = "Неизвестная команда";
    public const string StatsPlaceholder = "Здесь будет статистика";
    public const string SettingsPlaceholder = "Настройки бота";
    public const string ChooseAction = "Выберите действие:";
    public const string NoExpenses = "Пока нет трат";
    public const string LastExpensesHeader = "Последние траты:";

    public const string StartExpensePrompt =
        "Введите категорию, сумму и комментарий (опционально), например:\n" +
        "<b>Топливо 1500 Лукойл</b>\nДля отмены напишите: <b>/cancel</b>";

    public static string CategoriesList(IEnumerable<string> names) =>
        $"Список категорий: <b>{HtmlJoin(names)}</b>";

    public static string CategoryNotFound(IEnumerable<string> names) =>
        $"Такой категории нет. Доступные:\n<b>{HtmlJoin(names)}</b>\nДля отмены напишите: /cancel";

    public static string ExpenseAdded(decimal amount, string category, string? comment) =>
        $"Трата <b>{amount.ToString("0.##", CultureInfo.InvariantCulture)}₽</b> " +
        $"добавлена в категорию <b>{Html(category)}</b>" +
        (string.IsNullOrWhiteSpace(comment) ? "" : $"\nКомментарий: {Html(comment!)}");

    public static string ExpenseLine(DateTimeOffset whenUtc, string category, decimal amount, string? comment) =>
        $"{whenUtc:yyyy-MM-dd} — {Html(category)}: " +
        $"{amount.ToString("0.##", CultureInfo.InvariantCulture)}" +
        (string.IsNullOrWhiteSpace(comment) ? "" : $" ({Html(comment!)})");

    private static string Html(string s) => WebUtility.HtmlEncode(s);

    private static string HtmlJoin(IEnumerable<string> parts) =>
        Html(string.Join(", ", parts));

    public const string AboutBot =
        "<b>BudgetBuddyBot</b> — бот для личного учета расходов.\n\n" +
        "Что умеет:\n" +
        "• 🧾 Добавлять траты: <code>Категория Сумма [Комментарий]</code>\n" +
        "• 📒 Показывать последние траты\n" +
        "• 🗂️ Показать список категорий\n" +
        "• 📊 Статистика (в разработке)\n";
}
