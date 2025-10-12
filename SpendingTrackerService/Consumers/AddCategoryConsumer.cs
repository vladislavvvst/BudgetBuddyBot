using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Database.Entities;
using System.Text.RegularExpressions;

namespace SpendingTrackerService.Consumers;

internal sealed class AddCategoryConsumer : IConsumer<AddCategoryRequest>
{
    private readonly ILogger<AddCategoryConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AddCategoryConsumer(ILogger<AddCategoryConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<AddCategoryRequest> context)
    {
        AddCategoryRequest request = context.Message;
        CancellationToken ct = context.CancellationToken;

        string rawName = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rawName))
        {
            await context.RespondAsync(new AddCategoryResponse(false));
            return;
        }

        string name = Capitalize(CollapseSpaces(rawName));
        string normalized = name.ToLowerInvariant();

        bool activeExists = await ActiveExistsAsync(request.UserId, normalized, ct);
        if (activeExists)
        {
            await context.RespondAsync(new AddCategoryResponse(false));
            return;
        }

        CategoryEntity? deleted = await GetDeletedAsync(request.UserId, normalized, ct);
        bool restore = deleted is not null;

        if (restore)
        {
            // Перед восстановлением проверяем, что за время между запросом и восстановлением
            // не появилась активная категория с таким именем
            bool conflict = await ActiveExistsAsync(request.UserId, normalized, ct);
            if (conflict)
            {
                await context.RespondAsync(new AddCategoryResponse(false));
                return;
            }

            deleted!.IsDeleted = false;
        }
        else
        {
            await _dbContext.Categories.AddAsync(new CategoryEntity
            {
                UserId = request.UserId,
                Name = name,
                IsSystem = false,
                IsDeleted = false,
                RequestId = request.RequestId
            }, ct);
        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            await context.RespondAsync(new AddCategoryResponse(true));

            _logger.LogInformation("{Action} category '{Name}' for user {UserId}",
                restore ? "Restored" : "Added", name, request.UserId);

            IReadOnlyList<CategoryDto> items = await GetCategoriesFromDbAsync(request.UserId, ct);
            await context.Publish(new UserCategoriesChangedNotification(request.UserId, items), ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex,
                "Unique conflict while adding/restoring category '{Name}' for user {UserId}",
                name, request.UserId);

            bool nowExists = await ActiveExistsAsync(request.UserId, normalized, ct);
            await context.RespondAsync(new AddCategoryResponse(nowExists));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AddCategory failed for user {UserId}, name='{Name}'",
                request.UserId, name);

            await context.RespondAsync(new AddCategoryResponse(false));
        }
    }

    private Task<bool> ActiveExistsAsync(long userId, string normalizedLowerName, CancellationToken ct)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId
                           && !c.IsDeleted
                           && c.Name.ToLower() == normalizedLowerName, ct);
    }

    private Task<CategoryEntity?> GetDeletedAsync(long userId, string normalizedLowerName, CancellationToken ct)
    {
        return _dbContext.Categories
            .Where(c => c.UserId == userId
                        && c.IsDeleted
                        && !c.IsSystem
                        && c.Name.ToLower() == normalizedLowerName)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<IReadOnlyList<CategoryDto>> GetCategoriesFromDbAsync(long userId, CancellationToken ct)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.IsSystem))
            .ToListAsync(ct);
    }

    private static string CollapseSpaces(string s) => Regex.Replace(s, @"\s{2,}", " ").Trim();

    private static string Capitalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        input = input.Trim();
        return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
    }
}
