using MassTransit;
using Microsoft.EntityFrameworkCore;
using SpendingTrackerService.Database.Entities;

namespace SpendingTrackerService.Database;

internal class ApplicationDbContext : DbContext
{
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExpenseEntity>(entity =>
        {
            entity.ToTable("expenses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.CategoryId).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("numeric(19,2)").IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(512);
            entity.Property(x => x.AddedAtUtc).HasDefaultValueSql("now()").IsRequired();
            entity.Property(x => x.RequestId).HasMaxLength(128).IsRequired();

            // Связь: (UserId, CategoryId) → (UserId, Id)
            // Композитный FK на композитный AK (см. ниже) в CategoryEntity
            // для обеспечения принадлежности категории пользователю
            entity.HasOne(x => x.Category)
             .WithMany(c => c.Expenses)
             .HasForeignKey(x => new { x.UserId, x.CategoryId })
             .HasPrincipalKey(c => new { c.UserId, c.Id })
             .OnDelete(DeleteBehavior.Restrict); // Soft-delete категории, expenses остаются

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.CategoryId }); // Для выборки по категории
            entity.HasIndex(x => new { x.UserId, x.AddedAtUtc }); // Для выборки по дате
            entity.HasIndex(x => new { x.UserId, x.CategoryId, x.AddedAtUtc }); // Для выборки по категории и дате
            entity.HasIndex(x => new { x.UserId, x.RequestId }).IsUnique(); // Для идемпотентности
        });

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IsDeleted).IsRequired();
            entity.Property(x => x.IsSystem).IsRequired();
            entity.Property(x => x.AddedAtUtc).HasDefaultValueSql("now()").IsRequired();
            entity.Property(x => x.RequestId).HasMaxLength(128).IsRequired(false);

            entity.HasIndex(x => x.UserId);

            // Уникальность активных имен (soft-delete позволяет пересоздать)
            entity.HasIndex(x => new { x.UserId, x.Name })
                  .IsUnique()
                  .HasFilter("\"IsDeleted\" = false") // Частичный индекс только для активных
                  .HasDatabaseName("UX_categories_user_name_active"); // Имя индекса

            // Идемпотентность создания категории только для записей с RequestId
            entity.HasIndex(x => new { x.UserId, x.RequestId })
                  .IsUnique()
                  .HasFilter("\"RequestId\" IS NOT NULL")
                  .HasDatabaseName("UX_categories_user_request");

            // Системные нельзя удалять
            entity.ToTable(tb =>
                tb.HasCheckConstraint("CK_Category_NotSystemDeleted", "NOT (\"IsSystem\" AND \"IsDeleted\")"));

            // Альтернативный ключ для композитного FK
            entity.HasAlternateKey(x => new { x.UserId, x.Id })
                  .HasName("AK_categories_user_id_id");
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
