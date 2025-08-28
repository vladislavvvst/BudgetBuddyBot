using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Services;

namespace SpendingTrackerService.Consumers;

internal class GetCategoriesConsumer : IConsumer<GetCategoriesRequest>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICategorySeeder _seeder;

    public GetCategoriesConsumer(ApplicationDbContext dbContext, ICategorySeeder seeder)
        => (_dbContext, _seeder) = (dbContext, seeder);

    public async Task Consume(ConsumeContext<GetCategoriesRequest> context)
    {
        CancellationToken ct = context.CancellationToken;
        long userId = context.Message.UserId;

        // Гарантируем системные категории пользователю
        await _seeder.EnsureDefaultsAsync(userId, ct);

        List<CategoryDto> items = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.IsSystem))
            .ToListAsync(ct);

        await context.RespondAsync(new GetCategoriesResponse(userId, items));
    }
}
