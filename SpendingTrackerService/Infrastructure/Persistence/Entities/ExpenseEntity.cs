namespace SpendingTrackerService.Infrastructure.Persistence.Entities;

internal sealed class ExpenseEntity
{
    public long Id { get; set; } // PK
    public long UserId { get; set; } // FK → CategoryEntity.UserId
    public decimal Amount { get; set; } // Сумма
    public string? Comment { get; set; } // Комментарий, опционально
    public DateTime AddedAtUtc { get; set; } // Время добавления в UTC (Kind = Utc по умолчанию из Npgsql)

    public string RequestId { get; set; } = string.Empty; // Для идемпотентности (не nullable)
    public long CategoryId { get; set; } // FK → CategoryEntity.Id
    public CategoryEntity? Category { get; set; } // Навигационное свойство
}
