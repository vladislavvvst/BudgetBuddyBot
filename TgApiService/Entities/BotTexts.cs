using System.Globalization;
using System.Net;

namespace TgApiService.Entities;

internal static class BotTexts
{
    public const string CatMenu = "cat:menu";
    public const string CatStart = "cat:start";
    public const string CatAbout = "cat:about";
    public const string CatAdd = "cat:add";
    public const string CatDel = "cat:del";
    public const string CatDelPickPrefix = "cat:del:"; // + {categoryCode}
    public const string NavBack = "nav:back";

    public const string Start = "/start";
    public const string Menu = "/menu";
    public const string About = "/about";
    public const string Cancel = "/cancel";

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
    public const string ErrorAddingExpense = "Ошибка при добавлении траты. Попробуйте ещё раз";
    public const string ErrorAddingCategory = "Ошибка при добавлении категории. Попробуйте ещё раз";
    public const string CategoryAdd = "Введите название новой категории\n(или /cancel для отмены):";
    public const string CategoryDeleted = "Категория удалена";
    public const string ErrorDeletingCategory = "Ошибка при удалении категории. Попробуйте ещё раз";
    public const string NoCategories = "Нет категорий для удаления";
    public const string ChooseCategoryToDelete = "Выберите категорию для удаления:";
    public const string EmptyNameCategory = "Пустое имя категории. Попробуйте ещё раз\nДля отмены напишите: /cancel";
    public const string PushButton = "Сейчас нужно нажать кнопку на экране ⬇️";

    public const string ErrorProcessing = "Сервис упал при обработке. Попробуйте позже";
    public const string ErrorTimeout = "Таймаут запроса. Попробуйте ещё раз";

    public const string StartExpensePrompt =
        "Введите категорию, сумму и комментарий (опционально), например:\n" +
        "<b>Топливо 1500 Лукойл</b>\nДля отмены напишите: <b>/cancel</b>";

    public static string CategoriesList(IEnumerable<string> names) =>
        $"Список категорий:\n<b>{HtmlCategoriesJoin(names)}</b>";

    public static string CategotyAdded(string name) =>
        $"Категория <b>{Html(name)}</b> добавлена";

    public static string CategoryNotFound(IEnumerable<string> names) =>
        $"Такой категории нет. Доступные:\n<b>{HtmlJoin(names)}</b>\nДля отмены напишите: /cancel";

    public static string ExpenseAdded(decimal amount, string category, string? comment) =>
        $"Трата <b>{amount.ToString("0.##", CultureInfo.InvariantCulture)}₽</b> " +
        $"добавлена в категорию <b>{Html(category)}</b>" +
        (string.IsNullOrWhiteSpace(comment) ? "" : $"\nКомментарий: {Html(comment!)}");

    public static string ExpenseLine(DateTimeOffset addDate, string category, decimal amount, string? comment) =>
        $"{addDate:yyyy-MM-dd} — {Html(category)}: " +
        $"{amount.ToString("0.##", CultureInfo.InvariantCulture)}" +
        (string.IsNullOrWhiteSpace(comment) ? "" : $" ({Html(comment!)})");

    private static string Html(string s) => WebUtility.HtmlEncode(s);
    private static string HtmlJoin(IEnumerable<string> parts) => Html(string.Join(", ", parts));
    private static string HtmlCategoriesJoin(IEnumerable<string> parts) => Html(string.Join("\n", parts));

    public const string AboutBot =
        "<b>BudgetBuddyBot</b> — бот для личного учета расходов\n\n" +
        "Что умеет:\n" +
        "• ➕ Добавлять траты\n" +
        "• 📝 Показывать последние траты\n" +
        "• 🗂️ Показывать список категорий\n" +
        "• 🗂️ Изменять список категорий\n" +
        "• 📊 Показывать статистику\n";
}
