namespace TgApiService.Services;

internal enum UserState
{
    MainMenu = 0,
    ExpenseAdd_PickCategory = 1,
    ExpenseAdd_WaitAmountComment = 2,

    CategoryMenu = 10,
    CategoryAdd_WaitName = 11,
    CategoryDelete_WaitChoice = 12
};

internal interface IUserStateStorage
{
    Task<UserState> GetStateAsync(long chatId);
    Task SetStateAsync(long chatId, UserState state);

    // Временные значения (per-user, per-key)
    Task SetTempAsync(long chatId, string key, string value);
    Task<string?> GetTempAsync(long chatId, string key);
    Task RemoveTempAsync(long chatId, string key);
}
