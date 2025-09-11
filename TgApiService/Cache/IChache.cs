namespace TgApiService.Cache;

internal enum UserState
{
    MainMenu = 0,
    ExpenseAdd_PickCategory = 1,
    ExpenseAdd_WaitAmountComment = 2,

    CategoryMenu = 10,
    CategoryAdd_WaitName = 11,
    CategoryDelete_WaitChoice = 12
};

internal interface IStateCache
{
    // Состояния пользователя
    Task<UserState> GetStateAsync(long chatId);
    Task SetStateAsync(long chatId, UserState state);

    // Категория, выбранная пользователем при добавлении расхода
    Task<string?> GetCategoryIdAsync(long chatId);
    Task SetCategoryIdAsync(long chatId, string value);
    Task RemoveCategoryIdAsync(long chatId);

    // Флаг, что для пользователя уже добавлены системные категории (категории по умолчанию)
    Task<bool> GetDefaultCategoriesSeededAsync(long chatId);
    Task SetDefaultCategoriesSeededAsync(long chatId);
}
