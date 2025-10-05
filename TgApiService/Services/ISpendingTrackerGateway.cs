using SharedTypes;

namespace TgApiService.Services;

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
}
