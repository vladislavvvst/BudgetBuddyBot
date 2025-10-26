using System.Globalization;
using System.Net;
using System.Text;
using SharedTypes.Contracts;

namespace TgApiService.Presentation.Telegram.UI;

/// <summary>
/// Строковые константы для UI.
/// </summary>
internal static class UiStrings
{
    // ==============
    // Callback keys / prefixes
    // ==============
    internal static class CallbackData
    {
        public const string NavBack = "nav:back";

        public const string AddExpense = "exp:add";
        public const string Stats = "stats";
        public const string LastExpenses = "last_expenses";
        public const string Categories = "categories";

        public const string CatMenu = "cat:menu";
        public const string CatAdd = "cat:add";
        public const string CatDel = "cat:del";

        public const string CatDelPickPrefix = "cat:del:";  // + {categoryId}
        public const string ExpPickPrefix = "exp:pick:";    // + {categoryId}

        public const string StatsFullWeek = "stats:fullweek";
        public const string StatsToday = "stats:today";
        public const string Stats7Days = "stats:7days";
        public const string StatsMonth = "stats:month";
        public const string StatsRange = "stats:range";

        public const string StatsMetricTotalAmount = "stats:metric:total_amount";
        public const string StatsMetricByCategory  = "stats:metric:by_category";
        public const string StatsMetricDynByDays    = "stats:metric:dyn_by_days";
    }

    // ==============
    // Slash-команды
    // ==============
    internal static class Commands
    {
        public const string Start = "/start";
        public const string Menu = "/menu";
        public const string About = "/about";
        public const string Cancel = "/cancel";
    }

    // ==============
    // Кнопки
    // ==============
    internal static class Buttons
    {
        public const string Stats = "📊 Статистика";
        public const string AddExpense = "➕ Добавить трату";
        public const string LastExpenses = "📝 Последние траты";
        public const string Categories = "🗂️ Категории";
        public const string Add = "➕ Добавить";
        public const string Delete = "🗑️ Удалить";
        public const string Back = "⬅️ Назад";

        public const string BotStart = "Запустить бота";
        public const string BotMenu = "Открыть меню";
        public const string BotAbout = "О боте";

        public const string StatsFullWeek = "🔥 Полная статистика за 7 дней";
        public const string StatsToday    = "Сегодня";
        public const string Stats7Days    = "7 дней";
        public const string StatsMonth    = "Месяц";
        public const string StatsRange    = "📆 Диапазон";

        public const string StatsMetricTotalAmount = "💰 Общая сумма";
        public const string StatsMetricByCategory  = "🗂️ По категориям";
        public const string StatsMetricDynByDay    = "📈 Динамика по дням";
    }

    // ==============
    // Сообщения / подсказки
    // ==============
    internal static class Prompts
    {
        public const string ChooseAction = "Выберите действие:";

        public const string CategoryAdd = "Введите название новой категории:";
        public const string ChooseCategory = "Выберите категорию:";
        public const string ChooseCategoryToDelete = "Выберите категорию для удаления:";
        public const string ChoosePeriod = "Выберите период:";

        public const string StartText = "Привет! Я помогу вести расходы :)";
        public const string AboutBot =
            "<b>BudgetBuddyBot</b> — бот для личного учета расходов\n\n" +
            "Что умеет:\n" +
            "• ➕ Добавлять траты\n" +
            "• 📝 Показывать последние траты\n" +
            "• 🗂️ Показывать список категорий\n" +
            "• 🗂️ Изменять список категорий\n" +
            "• 📊 Показывать статистику\n";

        public const string StartExpensePrompt =
            "Введите сумму и комментарий (опционально), например:\n" +
            "<b>105590 Iphone 16 Pro Max 256 GB</b>";

        public const string SelectMetricPrompt = "Выберите метрику:";
    }

    // ==============
    // Сообщения об ошибках / статусы
    // ==============
    internal static class Errors
    {
        public const string AccessDeniedOwner      = "🚫 Доступ запрещен! Этот бот только для владельца";
        public const string AccessDeniedPrivate    = "🔒 Доступ запрещен! Бот работает только в личных сообщениях";

        public const string Cancelled              = "❌ Действие отменено";
        public const string EmptyInput             = "⚠️ Пустой ввод";
        public const string EmptyNameCategory      = "⚠️ Пустое имя категории";
        public const string TextExpected           = "⚠️ Ожидается текстовое сообщение";
        public const string StringTooLong          = "⚠️ Строка не должна превышать 64 символа";

        public const string BadFormat              = "🤔 Некорректный ввод\nФормат: <b>Сумма Комментарий (опционально)</b>\n" +
                                                     "Пример: <b>105590 Iphone 16 Pro Max 256 GB</b>";

        public const string BadAmount              = "🚫 Некорректная сумма\nВведите положительное число";
        public const string UnknownCmd             = "🤷‍♂️ Неизвестная команда";

        public const string ErrorAddingExpense     = "💥 Ошибка при добавлении траты";
        public const string ErrorAddingCategory    = "💥 Ошибка при добавлении категории";
        public const string ErrorDeletingCategory  = "💥 Ошибка при удалении категории";
        public const string ErrorProcessing        = "🔥 Сервис упал при обработке. Попробуйте позже";
        public const string ErrorTimeout           = "⏳ Таймаут запроса";

        public const string ErrorNameCategory      = "❓ Категория #";
    }

    // ==============
    // Нейтральные сообщения
    // ==============
    internal static class Info
    {
        public const string NoExpenses                  = "🪙 Пока нет трат";
        public const string LastExpensesHeader          = "📝 <b>Последние 10 трат:</b>";
        public const string NoCategories                = "📭 Нет категорий для удаления";
        public const string CategoryNotFoundForAddExp   = "⚠️ Категории не найдены. Сначала добавьте категорию ➕";
        public const string PushButton                  = "⚠️ Ожидается нажатие кнопки";
    }

    // ==============
    // Форматированные генераторы текстов
    // ==============
    public static string CategoriesList(IEnumerable<string> names) =>
        $"📂 <b>Список категорий:</b>\n{HtmlLines(names)}";

    public static string CategoryAdded(string name) =>
        $"✅ Категория <b>{HtmlText(name)}</b> успешно добавлена!";

    public static string CategoryDeleted(string name) =>
        $"🗑️ Категория <b>{HtmlText(name)}</b> успешно удалена!";

    public static string ExpenseAdded(decimal amount, string category, string? comment) =>
        $"💰 Трата <b>{Rub(amount)}</b> " +
        $"добавлена в категорию <b>{HtmlText(category)}</b> " +
        (string.IsNullOrWhiteSpace(comment)
            ? string.Empty
            : $"\n📝 Комментарий: {HtmlText(comment!)}");

    public static string ExpenseLine(DateTimeOffset addDate, string category, decimal amount, string? comment) =>
        $"📅 {addDate:dd.MM.yyyy}\n🗂️{HtmlText(category)}\n💰 <b>{Rub(amount)}</b>" +
        (string.IsNullOrWhiteSpace(comment)
            ? string.Empty
            : $"\n📝 {HtmlText(comment!)}");

    public static string Rub(decimal value) => value.ToString("N2", CultureInfo.CurrentCulture) + " ₽";

    // ==============
    // HTML helpers для Telegram
    // ==============
    // Экранируем только то, что отображается пользователю (текст), НЕ url в href
    public static string HtmlText(string s) => WebUtility.HtmlEncode(s);

    public static string Bold(string s) => $"<b>{HtmlText(s)}</b>";

    public static string Code(string s) => $"<code>{HtmlText(s)}</code>";

    // Ссылка: если text не задан — показываем сам url
    public static string Link(string url, string? text = null)
    {
        // href оставляем как есть, но текст внутри <a> энкодим
        string visible = string.IsNullOrWhiteSpace(text) ? url : text!;
        return $"<a href=\"{url}\">{HtmlText(visible)}</a>";
    }

    public static string HtmlJoin(IEnumerable<string> parts, string separator) =>
        HtmlText(string.Join(separator, parts));

    private static string HtmlLines(IEnumerable<string> lines) =>
        HtmlText(string.Join("\n", lines));

    // Форматированный вывод для страниц
    public static string FormatFullWeekStats(GetStatsFullWeekResponse stats)
    {
        StringBuilder sb = new();

        DateOnly startDay = DateOnly.FromDateTime(DateTime.Now);
        DateOnly endDay = startDay.AddDays(-6);

        sb.AppendLine($"📊 <b>Неделя: {startDay:dd.MM}–{endDay:dd.MM}</b>");
        sb.AppendLine();

        Summary s = stats.Summary;
        sb.AppendLine($"💰 Всего: <b>{Rub(s.Total)}</b>");
        sb.AppendLine($"📉 В день: <b>{Rub(s.AvgPerDay)}</b>");

        if (s.LargestExpenseDay.Amount > 0m)
        {
            string label = string.IsNullOrWhiteSpace(s.LargestExpenseDay.Comment)
                ? string.Empty
                : $" — {HtmlText(s.LargestExpenseDay.Comment)}";
            string dayStr = s.LargestExpenseDay.Day == default ? string.Empty : $" ({s.LargestExpenseDay.Day:dd.MM})";
            sb.AppendLine($"🔥 Крупнейшая трата: <b>{Rub(s.LargestExpenseDay.Amount)}</b>{label}{dayStr}");
        }

        if (s.DailyAmount.Amount > 0m && s.DailyAmount.Day != default)
            sb.AppendLine($"🔥 Самый затратный день: {s.DailyAmount.Day:dd.MM} — <b>{Rub(s.DailyAmount.Amount)}</b>");

        sb.AppendLine();

        sb.AppendLine("📂 <b>Топ категорий</b>");
        if (stats.CategoriesTop5 is { Count: > 0 })
        {
            decimal shownTotal = 0m;
            foreach (TotalSpendByCategory item in stats.CategoriesTop5)
            {
                string name = HtmlText(item.Name);
                shownTotal += item.Amount;
                sb.AppendLine($"• {name} — <b>{Rub(item.Amount)}</b>");
            }

            if (!string.IsNullOrWhiteSpace(s.TotalSpendByCategory.Name) && s.TotalSpendByCategory.Amount > 0m)
                sb.AppendLine($"🔥 Крупнейшая категория: {HtmlText(s.TotalSpendByCategory.Name)} — {Rub(s.TotalSpendByCategory.Amount)}");

            decimal leftover = Math.Max(0m, s.Total - shownTotal);
            if (leftover > 0m)
                sb.AppendLine($"• Прочее — <b>{Rub(leftover)}</b>");
        }
        else
        {
            sb.AppendLine("— нет данных");
        }

        sb.AppendLine();

        // Динамика по дням (7 строк, хронологически)
        sb.AppendLine("📈 <b>Динамика по дням</b>");
        if (stats.Days is { Count: > 0 })
        {
            foreach (DailyAmount d in stats.Days.OrderBy(d => d.Day))
                sb.AppendLine($"• {d.Day:dd.MM} — <b>{Rub(d.Amount)}</b>");
        }
        else
        {
            sb.AppendLine("— нет данных");
        }

        return sb.ToString();
    }

    public static string FormatTopCategories(GetStatsTopCategoryResponse response, int maxItems = 10, string currencySymbol = "₽")
    {
        StringBuilder sb = new(256);

        sb.AppendLine("<b>🏆 Топ категорий</b>");

        if (response?.Categories is null || response.Categories.Count == 0)
        {
            sb.Append("Нет данных");
            return sb.ToString();
        }

        int total = response.Categories.Count;
        int take = Math.Max(0, Math.Min(maxItems, total));
        if (take == 0)
        {
            sb.Append("Нет данных");
            return sb.ToString();
        }

        sb.AppendLine();

        string[] medals = ["🥇", "🥈", "🥉"];

        int topCount = Math.Min(3, take);
        for (int i = 0; i < topCount; i++)
        {
            TotalSpendByCategory dto = response.Categories[i];
            string name = WebUtility.HtmlEncode(dto.Name);
            string amount = Rub(dto.Amount);

            sb.Append(medals[i]).Append(' ')
                .Append("<b>").Append(name).Append("</b>")
                .Append(" — ").Append(amount).Append('\n');
        }

        sb.AppendLine();

        if (take > topCount)
        {
            sb.Append("<i>Остальные:</i>").Append('\n');

            for (int i = topCount; i < take; i++)
            {
                TotalSpendByCategory dto = response.Categories[i];
                string name = WebUtility.HtmlEncode(dto.Name);
                string amount = Rub(dto.Amount);

                sb.Append("• ").Append(name)
                    .Append(" — ").Append(amount).Append('\n');
            }
        }

        return sb.ToString();
    }

    public static string FormatDaysHtml(GetStatsDaysResponse response)
    {
        IReadOnlyList<DailyAmount> days = response?.Days ?? [];
        StringBuilder sb = new();

        sb.AppendLine("📊 <b>Динамика по дням</b>");

        if (days.Count == 0)
        {
            sb.Append("Нет данных");
            return sb.ToString();
        }

        sb.AppendLine();

        List<DailyAmount> ordered = days.OrderBy(d => d.Day).ToList();

        foreach (DailyAmount item in ordered)
        {
            string date = item.Day.ToString("dd.MM", CultureInfo.CurrentCulture);
            string amount = Rub(item.Amount);
            sb.Append("• <b>").Append(date).Append("</b> — ").AppendLine(amount);
        }

        decimal total = ordered.Sum(d => d.Amount);
        int count = days.Count;
        decimal average = count > 0 ? total / count : 0m;

        sb.AppendLine();
        sb.Append("<b>Итого:</b> ").AppendLine(Rub(total));
        sb.Append("<b>Среднее в день:</b> ").Append(Rub(average));

        return sb.ToString();
    }

    public static string FormatTotalAmountStats(GetStatsAmountResponse response)
    {
        string largestNote = string.IsNullOrWhiteSpace(response.LargestExpenseDay?.Comment)
            ? "без комментария"
            : response.LargestExpenseDay!.Comment;

        string largest =
            response.LargestExpenseDay is { Amount: > 0 } le
                ? $"🔥 Крупнейшая трата: {Rub(le.Amount)} — {largestNote}"
                : "🔥 Крупнейшая трата: —";

        return $"💰 Всего расходов: {Rub(response.Amount)}\n📈 Средний расход в день: {Rub(response.AvgPerDay)}\n{largest}";
    }
}
