namespace TgApiService.Services;

internal enum UserState
{
    MainMenu = 0,
    ExpenseAdd_WaitExpense = 1,

    CategoryMenu = 10,
    CategoryAdd_WaitName = 11,
    CategoryDelete_WaitChoice = 12
};

internal interface IUserStateStorage
{
    Task<UserState> GetStateAsync(long chatId);
    Task SetStateAsync(long chatId, UserState state);
}
