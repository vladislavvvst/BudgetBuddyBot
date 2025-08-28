using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Database.Entities;
using SpendingTrackerService.Services;
using System.Text.RegularExpressions;

namespace SpendingTrackerService.Consumers;

internal class AddCategoryConsumer : IConsumer<AddCategoryRequest>
{
    private readonly ILogger<AddCategoryConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICategorySeeder _seeder;

    public AddCategoryConsumer(ILogger<AddCategoryConsumer> logger, ApplicationDbContext dbContext, ICategorySeeder seeder) =>
        (_logger, _dbContext, _seeder) = (logger, dbContext, seeder);

    public async Task Consume(ConsumeContext<AddCategoryRequest> context)
    {
        AddCategoryRequest request = context.Message;
        CancellationToken ct = context.CancellationToken;

        // Гарантируем системные категории пользователю
        await _seeder.EnsureDefaultsAsync(request.UserId, ct);

        // Нормализация ввода
        string? rawName = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rawName))
        {
            await context.RespondAsync(new AddCategoryResponse(false));
            return;
        }
        string name = CollapseSpaces(rawName);

        try
        {
            // Уже есть активная с таким именем?
            bool activeExists = await _dbContext.Categories
                .AnyAsync(c => c.UserId == request.UserId && !c.IsDeleted && c.Name == name, ct);
            if (activeExists)
            {
                await context.RespondAsync(new AddCategoryResponse(false));
                return;
            }

            // Если ранее удаляли пользовательскую с тем же именем — вернем обратно
            CategoryEntity? deleted = await _dbContext.Categories.SingleOrDefaultAsync(
                c => c.UserId == request.UserId && c.IsDeleted && !c.IsSystem && c.Name == name, ct);

            if (deleted is not null)
            {
                deleted.IsDeleted = false;
                await _dbContext.SaveChangesAsync(ct);
                await context.RespondAsync(new AddCategoryResponse(true));
                return;
            }

            // Создаем новую пользовательскую
            _dbContext.Categories.Add(new CategoryEntity
            {
                UserId = request.UserId,
                Name = name,
                IsSystem = false,
                IsDeleted = false,
                AddedAtUtc = DateTimeOffset.UtcNow,
                RequestId = request.RequestId
            });

            await _dbContext.SaveChangesAsync(ct);
            await context.RespondAsync(new AddCategoryResponse(true));
        }
        catch (DbUpdateException ex)
        {
            // Возможная гонка уникального индекса (UserId, Name) по активным
            _logger.LogWarning(ex, "Unique conflict while adding category '{Name}' for user {UserId}", name, request.UserId);

            bool nowExists = await _dbContext.Categories
                .AnyAsync(c => c.UserId == request.UserId && !c.IsDeleted && c.Name == name, ct);

            await context.RespondAsync(new AddCategoryResponse(nowExists));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddCategory failed for user {UserId}, name='{Name}'", request.UserId, name);
            await context.RespondAsync(new AddCategoryResponse(false));
        }
    }

    private static string CollapseSpaces(string s) => Regex.Replace(s, @"\s{2,}", " ").Trim();
}
