namespace SpendingTrackerService.Infrastructure.Persistence.Entities;

internal sealed class CategoryEntity
{
    public long Id { get; set; } // PK
    public long UserId { get; set; } // FK (для фильтрации по пользователю)
    public string Name { get; set; } = string.Empty; // Имя категории (citext)
    public bool IsDeleted { get; set; } // Soft-delete
    public bool IsSystem { get; set; } // Системная категория (нельзя удалять)
    public DateTime AddedAtUtc { get; set; } // Время добавления в UTC (Kind = Utc по умолчанию из Npgsql)

    public string? RequestId { get; set; } // Для идемпотентности (nullable, только для AddCategory)
    public ICollection<ExpenseEntity> Expenses { get; set; } = []; // Навигационное свойство
}
