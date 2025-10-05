using SharedTypes;

namespace TgApiService.Cache;

/// <summary>
/// Перечисление всех сцен бота.
/// Определяет, на каком шаге пользователь сейчас находится.
/// </summary>
internal enum UserState
{
    MainMenu = 0,                       // Главное меню
    ExpenseAddPickCategory = 1,         // Выбор категории при добавлении траты
    ExpenseAddWaitAmountComment = 2,    // Ввод суммы и комментария

    CategoryMenu = 10,                  // Меню категорий
    CategoryAddWaitName = 11,           // Ввод имени новой категории
    CategoryDeleteWaitChoice = 12,      // Выбор категории для удаления

    StatisticsWaitPeriod = 20,          // Выбор периода статистики
    StatisticsWaitChoice = 21,          // Выбор типа статистики
    StatisticsWaitRangeInput = 22       // Ввод диапазона дат для статистики
};

/// <summary>
/// Тип входящего update от Telegram.
/// </summary>
internal enum UpdateKind { Unknown, Message, CallbackQuery, Command }

/// <summary>
/// Интерфейс хранилища пользовательского состояния и вспомогательных данных.
/// </summary>
internal interface IStateCache
{
    // Состояния пользователя (в какой сцене находится)
    Task<UserState> GetStateAsync(long chatId);
    Task SetStateAsync(long chatId, UserState state);

    // Временно выбранная категория (пока пользователь добавляет трату)
    Task<string?> GetCategoryIdAsync(long chatId);
    Task SetCategoryIdAsync(long chatId, string value);
    Task RemoveCategoryIdAsync(long chatId);

    // Кэш списка категорий пользователя, чтобы не ходить за ними в сервис каждый раз
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(long chatId);
    Task SetCategoriesAsync(long chatId, IReadOnlyList<CategoryDto> categories);
}
