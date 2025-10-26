using MassTransit;
using SharedTypes.Contracts;
using TgApiService.Application.Abstractions;

namespace TgApiService.Infrastructure.Gateways;

/// <summary>
/// Реализация шлюза через MassTransit-шину.
/// Для каждого типа запроса создает IRequestClient и ждет ответа.
/// </summary>
internal sealed class SpendingTrackerGateway : ISpendingTrackerGateway
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

    public async Task<GetStatsFullWeekResponse> GetStatsFullWeekAsync(GetStatsFullWeekRequest request, CancellationToken ct)
    {
        IRequestClient<GetStatsFullWeekRequest> client = _clients.CreateRequestClient<GetStatsFullWeekRequest>();
        Response<GetStatsFullWeekResponse> response = await client.GetResponse<GetStatsFullWeekResponse>(request, ct);
        return response.Message;
    }

    public async Task<GetStatsAmountResponse> GetStatsAmountAsync(GetStatsAmountRequest request, CancellationToken ct)
    {
        IRequestClient<GetStatsAmountRequest> client = _clients.CreateRequestClient<GetStatsAmountRequest>();
        Response<GetStatsAmountResponse> response = await client.GetResponse<GetStatsAmountResponse>(request, ct);
        return response.Message;
    }

    public async Task<GetStatsTopCategoryResponse> GetStatsTopCategoryAsync(GetStatsTopCategoryRequest request, CancellationToken ct)
    {
        IRequestClient<GetStatsTopCategoryRequest> client = _clients.CreateRequestClient<GetStatsTopCategoryRequest>();
        Response<GetStatsTopCategoryResponse> response = await client.GetResponse<GetStatsTopCategoryResponse>(request, ct);
        return response.Message;
    }

    public async Task<GetStatsDaysResponse> GetStatsDaysAsync(GetStatsDaysRequest request, CancellationToken ct)
    {
        IRequestClient<GetStatsDaysRequest> client = _clients.CreateRequestClient<GetStatsDaysRequest>();
        Response<GetStatsDaysResponse> response = await client.GetResponse<GetStatsDaysResponse>(request, ct);
        return response.Message;
    }
}
