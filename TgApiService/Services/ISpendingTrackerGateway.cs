using SharedTypes;

namespace TgApiService.Services;

internal interface ISpendingTrackerGateway
{
    // Траты
    Task<AddExpenseResponse> AddExpenseAsync(AddExpenseRequest req, CancellationToken ct);
    Task<GetExpensesResponse> GetExpensesAsync(GetExpensesRequest req, CancellationToken ct);

    // Категории
    Task<AddCategoryResponse> AddCategoryAsync(AddCategoryRequest req, CancellationToken ct);
    Task<GetCategoriesResponse> GetCategoriesAsync(GetCategoriesRequest req, CancellationToken ct);
    Task<DeleteCategoryResponse> DeleteCategoryAsync(DeleteCategoryRequest req, CancellationToken ct);
}
