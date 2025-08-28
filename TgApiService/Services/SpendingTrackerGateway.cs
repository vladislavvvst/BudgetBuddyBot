using MassTransit;
using SharedTypes;

namespace TgApiService.Services;

internal class SpendingTrackerGateway : ISpendingTrackerGateway
{
    private readonly IClientFactory _clients;

    public SpendingTrackerGateway(IClientFactory clients) => _clients = clients;

    public async Task<AddExpenseResponse> AddExpenseAsync(AddExpenseRequest request, CancellationToken ct)
    {
        IRequestClient<AddExpenseRequest> client = _clients.CreateRequestClient<AddExpenseRequest>();
        Response<AddExpenseResponse> response = await client.GetResponse<AddExpenseResponse>(request, ct);
        return response.Message;
    }

    public async Task<GetExpensesResponse> GetExpensesAsync(GetExpensesRequest request, CancellationToken ct)
    {
        IRequestClient<GetExpensesRequest> client = _clients.CreateRequestClient<GetExpensesRequest>();
        Response<GetExpensesResponse> response = await client.GetResponse<GetExpensesResponse>(request, ct);
        return response.Message;
    }

    public async Task<AddCategoryResponse> AddCategoryAsync(AddCategoryRequest request, CancellationToken ct)
    {
        IRequestClient<AddCategoryRequest> client = _clients.CreateRequestClient<AddCategoryRequest>();
        Response<AddCategoryResponse> response = await client.GetResponse<AddCategoryResponse>(request, ct);
        return response.Message;
    }

    public async Task<GetCategoriesResponse> GetCategoriesAsync(GetCategoriesRequest request, CancellationToken ct)
    {
        IRequestClient<GetCategoriesRequest> client = _clients.CreateRequestClient<GetCategoriesRequest>();
        Response<GetCategoriesResponse> response = await client.GetResponse<GetCategoriesResponse>(request, ct);
        return response.Message;
    }

    public async Task<DeleteCategoryResponse> DeleteCategoryAsync(DeleteCategoryRequest request, CancellationToken ct)
    {
        IRequestClient<DeleteCategoryRequest> client = _clients.CreateRequestClient<DeleteCategoryRequest>();
        Response<DeleteCategoryResponse> response = await client.GetResponse<DeleteCategoryResponse>(request, ct);
        return response.Message;
    }
}
