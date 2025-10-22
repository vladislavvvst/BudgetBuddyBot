using SharedTypes;

namespace TgApiService.Application.Abstractions;

/// <summary>
/// Перечисление всех сцен бота.
/// Определяет, на каком шаге пользователь сейчас находится.
/// </summary>
internal enum UserState
{
    // === Главное меню ===
    MainMenu = 0,                       // Главное меню

    // === Добавление трат ===
    ExpensePickCategory = 10,           // Выбор категории при добавлении траты
    ExpenseAmountComment = 11,          // Ввод суммы и комментария
    ExpenseShowLast = 12,               // Показать последние траты

    // === Категории ===
    CategoryMenu = 20,                  // Меню категорий
    CategoryAddName = 21,               // Ввод имени новой категории
    CategoryDelete = 22,                // Выбор категории для удаления

    // === Статистика ===
    StatisticsPeriod = 30,              // Ожидание выбора периода
    StatisticsMetric = 31,              // Ожидание выбора типа статистики (метрики)
    StatsFullWeek = 32,                 // Показать статистику за неделю
}

/// <summary>
/// Интерфейс хранилища пользовательского состояния и вспомогательных данных.
/// </summary>
internal interface IStateCache
{
    // Состояния пользователя (в какой сцене находится)
    Task<UserState> GetStateAsync(long chatId);
    Task SetStateAsync(long chatId, UserState state);

    // Временно выбранная категория (пока пользователь добавляет трату)
    Task<long?> GetCategoryIdAsync(long chatId);
    Task SetCategoryIdAsync(long chatId, long categoryId);
    Task RemoveCategoryIdAsync(long chatId);

    // Временно выбранный период для статистики
    Task<string?> GetStatsPeriod(long chatId);
    Task SetStatsPeriod(long chatId, string period);
    Task RemoveStatsPeriod(long chatId);

    // Кэш списка категорий пользователя, чтобы не ходить за ними в сервис каждый раз
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(long chatId);
    Task SetCategoriesAsync(long chatId, IReadOnlyList<CategoryDto> categories);
}
