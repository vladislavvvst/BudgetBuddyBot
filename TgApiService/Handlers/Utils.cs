using SharedTypes;

namespace TgApiService.Handlers;

internal static class Utils
{
    public static long ChatId(HandlerContext context) =>
        context.Update.Message?.Chat.Id ?? context.Update.CallbackQuery!.Message!.Chat.Id;

    public static async Task<IReadOnlyList<CategoryDto>> GetUserCategories(HandlerContext context, CancellationToken ct)
    {
        // Запрашиваем список категорий из кэша
        // Если в кэше нет, то они будут запрошены у SpendingTrackerService (RPC)
        IReadOnlyList<CategoryDto> categories = await context.StateCache.GetCategoriesAsync(ChatId(context));

        if (categories.Count == 0)
        {
            categories = (await context.Tracker.GetCategoriesAsync(new(ChatId(context)), ct)).Items;

            context.Logger.LogInformation("Loaded {Count} categories for user {UserId} from SpendingTrackerService in cache",
                categories.Count, ChatId(context));

            // Обновляем кэш
            await context.StateCache.SetCategoriesAsync(ChatId(context), categories);
        }
        return categories;
    }
}
