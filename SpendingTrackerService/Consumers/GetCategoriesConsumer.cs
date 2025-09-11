using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;

namespace SpendingTrackerService.Consumers;

internal class GetCategoriesConsumer : IConsumer<GetCategoriesRequest>
{
    private readonly ApplicationDbContext _dbContext;

    public GetCategoriesConsumer(ApplicationDbContext dbContext)
        => (_dbContext) = (dbContext);

    public async Task Consume(ConsumeContext<GetCategoriesRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        long userId = context.Message.UserId;

        List<CategoryDto> items = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.IsSystem))
            .ToListAsync(ct);

        await context.RespondAsync(new GetCategoriesResponse(userId, items));
    }
}
