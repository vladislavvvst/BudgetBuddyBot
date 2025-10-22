namespace SpendingTrackerService.Infrastructure.Persistence.Entities;

internal sealed class CategoryEntity
{
    public long Id { get; set; } // PK
    public long UserId { get; set; } // FK → User
    public string Name { get; set; } = string.Empty; // Имя категории
    public bool IsDeleted { get; set; } // Soft-delete
    public bool IsSystem { get; set; } // Системная категория (нельзя удалять)
    public DateTimeOffset AddedAtUtc { get; set; } // Время добавления в UTC

    public string? RequestId { get; set; } // Для идемпотентности (только для AddCategory)
    public ICollection<ExpenseEntity> Expenses { get; set; } = []; // Навигационное свойство
}
