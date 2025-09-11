using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Database.Entities;
using System.Text.RegularExpressions;

namespace SpendingTrackerService.Consumers;

internal class AddCategoryConsumer : IConsumer<AddCategoryRequest>
{
    private readonly ILogger<AddCategoryConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AddCategoryConsumer(ILogger<AddCategoryConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<AddCategoryRequest> context)
    {
        AddCategoryRequest request = context.Message;
        CancellationToken ct = context.CancellationToken;

        // Нормализация ввода
        string? rawName = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rawName))
        {
            await context.RespondAsync(new AddCategoryResponse(false));
            return;
        }

        string name = CollapseSpaces(rawName);
        string normalized = name.ToLowerInvariant();

        try
        {
            // Уже есть активная с таким именем? (сравнение без учета регистра, "Дом" == "дом" -> true)
            bool activeExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(c => c.UserId == request.UserId &&
                               !c.IsDeleted &&
                               c.Name.ToLower() == normalized, ct);

            if (activeExists)
            {
                await context.RespondAsync(new AddCategoryResponse(false));
                return;
            }

            // Если ранее удаляли пользовательскую с тем же именем — вернем обратно
            CategoryEntity? deleted = await _dbContext.Categories
                .Where(c => c.UserId == request.UserId &&
                            c.IsDeleted &&
                            !c.IsSystem &&
                            c.Name.ToLower() == normalized)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(ct);

            // Восстанавливаем удаленную
            if (deleted is not null)
            {
                // Проверка на конфликт, т.к. между выборкой и сохранением могла быть гонка
                bool conflict = await _dbContext.Categories
                    .AsNoTracking()
                    .AnyAsync(c => c.UserId == request.UserId &&
                                   !c.IsDeleted &&
                                   c.Name.ToLower() == normalized, ct);

                if (conflict)
                {
                    await context.RespondAsync(new AddCategoryResponse(false));
                    return;
                }

                deleted.IsDeleted = false;
                await _dbContext.SaveChangesAsync(ct);
                await context.RespondAsync(new AddCategoryResponse(true));
                return;
            }

            // Создаем новую пользовательскую
            CategoryEntity newCategory = new()
            {
                UserId = request.UserId,
                Name = Capitalize(name),
                IsSystem = false,
                IsDeleted = false,
                RequestId = request.RequestId
            };

            _dbContext.Categories.Add(newCategory);
            await _dbContext.SaveChangesAsync(ct);

            await context.RespondAsync(new AddCategoryResponse(true));
        }
        catch (DbUpdateException ex)
        {
            // Возможная гонка уникального индекса (UserId, Name) по активным
            _logger.LogWarning(ex, "Unique conflict while adding category '{Name}' for user {UserId}", name, request.UserId);

            bool nowExists = await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(c => c.UserId == request.UserId &&
                               !c.IsDeleted &&
                               c.Name.ToLower() == normalized, ct);

            await context.RespondAsync(new AddCategoryResponse(nowExists));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddCategory failed for user {UserId}, name='{Name}'", request.UserId, name);
            await context.RespondAsync(new AddCategoryResponse(false));
        }
    }

    private static string CollapseSpaces(string s) => Regex.Replace(s, @"\s{2,}", " ").Trim();

    // Простая капитализация: "доМ" -> "Дом"
    private static string Capitalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        input = input.Trim();
        return char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
    }
}
