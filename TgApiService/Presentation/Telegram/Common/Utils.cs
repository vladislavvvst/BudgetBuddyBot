using MassTransit;
using SharedTypes;

namespace TgApiService.Presentation.Telegram.Common;

/// <summary>
/// Вспомогательные методы, часто используемые сценами.
/// </summary>
internal static class Utils
{
    public static long ChatId(UpdateContext context) =>
        context.Update.Message?.Chat.Id ?? context.Update.CallbackQuery!.Message!.Chat.Id;

    public static async Task<IReadOnlyList<CategoryDto>> GetUserCategories(UpdateContext context, CancellationToken ct)
    {
        long chatId = ChatId(context);

        IReadOnlyList<CategoryDto> categories = await context.StateCache.GetCategoriesAsync(chatId);

        if (categories.Count > 0)
            return categories;

        try
        {
            GetCategoriesResponse response = await context.Tracker.GetCategoriesAsync(new(chatId), ct);
            categories = response.Items;

            context.Logger.LogInformation("Loaded {Count} categories for user {UserId} from SpendingTrackerService", categories.Count, chatId);

            if (categories.Count > 0)
                await context.StateCache.SetCategoriesAsync(chatId, categories);

            return categories;
        }
        catch (RequestTimeoutException)
        {
            context.Logger.LogWarning("GetCategories timeout for user {UserId}", chatId);
            return [];
        }
        catch (RequestFaultException ex)
        {
            context.Logger.LogError(ex, "GetCategories fault for user {UserId}", chatId);
            return [];
        }
    }
}
