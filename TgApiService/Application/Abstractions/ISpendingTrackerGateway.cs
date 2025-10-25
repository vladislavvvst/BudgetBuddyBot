using SharedTypes.Contracts;

namespace TgApiService.Application.Abstractions;

/// <summary>
/// Интерфейс RPC-шлюза - работа с шиной сообщений (MassTransit/IRequestClient).
/// </summary>
internal interface ISpendingTrackerGateway
{
    // Траты
    Task<AddExpenseResponse> AddExpenseAsync(AddExpenseRequest request, CancellationToken ct);
    Task<GetExpensesResponse> GetExpensesAsync(GetExpensesRequest request, CancellationToken ct);

    // Категории
    Task<AddCategoryResponse> AddCategoryAsync(AddCategoryRequest request, CancellationToken ct);
    Task<GetCategoriesResponse> GetCategoriesAsync(GetCategoriesRequest request, CancellationToken ct);
    Task<DeleteCategoryResponse> DeleteCategoryAsync(DeleteCategoryRequest request, CancellationToken ct);

    // Статистика
    Task<GetStatsFullWeekResponse> GetStatsFullWeekAsync(GetStatsFullWeekRequest request, CancellationToken ct);
    Task<GetStatsAmountResponse> GetStatsAmountAsync(GetStatsAmountRequest request, CancellationToken ct);
}
