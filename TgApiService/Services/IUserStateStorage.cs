namespace TgApiService.Services;

internal enum UserState { None, WaitAddExpense };

internal interface IUserStateStorage
{
    Task<UserState> GetStateAsync(long chatId);
    Task SetStateAsync(long chatId, UserState state);
}
