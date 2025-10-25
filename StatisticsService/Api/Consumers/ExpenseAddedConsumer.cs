using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedTypes.Contracts;
using StatisticsService.Infrastructure.Persistence;
using StatisticsService.Infrastructure.Persistence.Entities;

namespace StatisticsService.Api.Consumers;

internal sealed class ExpenseAddedConsumer : IConsumer<ExpenseAddedNotification>
{
    private readonly ILogger<ExpenseAddedConsumer> _logger;
    private readonly ApplicationDbContext _dbContext;

    public ExpenseAddedConsumer(ILogger<ExpenseAddedConsumer> logger, ApplicationDbContext dbContext)
        => (_logger, _dbContext) = (logger, dbContext);

    public async Task Consume(ConsumeContext<ExpenseAddedNotification> context)
    {
        CancellationToken ct = context.CancellationToken;
        ExpenseAddedNotification message = context.Message;

        if (message?.Expense is null || message.Expense.Amount <= 0m)
        {
            _logger.LogWarning("Skip invalid ExpenseAddedNotification. MessageId={MessageId}", context.MessageId);
            return;
        }

        DateOnly day = DateOnly.FromDateTime(message.Expense.AddedAtUtc.UtcDateTime);

        long userId = message.UserId;
        long categoryId = message.Category.Id;
        string categoryName = message.Category.Name;
        decimal amount = message.Expense.Amount;
        string? note = message.Expense.Comment;

        // Ищем запись в таблице на текущий день. Если не найдена - добавляем, если найдена - обновляем данные.
        StatsDailyEntity? daily = await _dbContext.StatsDaily
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Day == day, ct);

        StatsDailyByCategoryEntity? dailyByCat = await _dbContext.StatsDailyByCategory
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Day == day && x.CategoryId == categoryId, ct);

        if (daily is null)
        {
            daily = new StatsDailyEntity
            {
                UserId = userId,
                Day = day,
                AmountTotal = amount,
                ExpensesCount = 1,
                MaxExpenseAmount = amount,
                MaxExpenseNote = note
            };
            _dbContext.Add(daily);
        }
        else
        {
            daily.AmountTotal += amount;
            daily.ExpensesCount += 1;

            // Обновление максимальной суммы и комментария самой большой траты
            if (amount > daily.MaxExpenseAmount)
            {
                daily.MaxExpenseAmount = amount;
                daily.MaxExpenseNote = note;
            }

            daily.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        if (dailyByCat is null)
        {
            dailyByCat = new StatsDailyByCategoryEntity
            {
                UserId = userId,
                Day = day,
                CategoryId = categoryId,
                CategoryName = categoryName,
                AmountTotal = amount,
                ExpensesCount = 1,
                MaxExpenseAmount = amount,
                MaxExpenseNote = note
            };
            _dbContext.Add(dailyByCat);
        }
        else
        {
            dailyByCat.AmountTotal += amount;
            dailyByCat.ExpensesCount += 1;

            if (amount > dailyByCat.MaxExpenseAmount)
            {
                dailyByCat.MaxExpenseAmount = amount;
                dailyByCat.MaxExpenseNote = note;
            }

            dailyByCat.CategoryName = categoryName;
            dailyByCat.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning("Failed to update stats. UserId={UserId}; Day={Day}; CategoryId={CategoryId}; MessageId={MessageId}",
                userId, day, categoryId, context.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update stats. UserId={UserId}; Day={Day}; CategoryId={CategoryId}; MessageId={MessageId}",
                userId, day, categoryId, context.MessageId);
        }
    }
}
