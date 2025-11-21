using MassTransit;
using Microsoft.EntityFrameworkCore;
using SpendingTrackerService.Infrastructure.Persistence.Entities;

namespace SpendingTrackerService.Infrastructure.Persistence;

internal sealed class ApplicationDbContext : DbContext
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
            entity.Property(x => x.AddedAtUtc)
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd()
                .IsRequired();
            entity.Property(x => x.RequestId).HasMaxLength(128).IsRequired();

            // Связь: (UserId, CategoryId) → (UserId, Id)
            // Композитный FK на композитный AK (см. ниже) в CategoryEntity
            // для обеспечения принадлежности категории пользователю
            // FK: (UserId, CategoryId) -> (UserId, Id) (альтернативный ключ у Category)
            entity.HasOne(x => x.Category)
                .WithMany(c => c.Expenses)
                .HasForeignKey(x => new { x.UserId, x.CategoryId })
                .HasPrincipalKey(c => new { c.UserId, c.Id })
                .OnDelete(DeleteBehavior.Restrict); // Soft-delete категории, expenses остаются

            entity.HasIndex(x => x.UserId);

            entity.HasIndex(x => new { x.UserId, x.AddedAtUtc })
                .HasDatabaseName("IX_expenses_user_added_desc")
                .IsDescending();

            entity.HasIndex(x => new { x.UserId, x.CategoryId, x.AddedAtUtc })
                .HasDatabaseName("IX_expenses_user_cat_added_desc")
                .IsDescending();

            entity.HasIndex(x => new { x.UserId, x.CategoryId })
                .HasDatabaseName("IX_expenses_user_cat");

            // Идемпотентность: один RequestId на пользователя
            entity.HasIndex(x => new { x.UserId, x.RequestId })
                .IsUnique()
                .HasDatabaseName("UX_expenses_user_request");
        });

        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Name).HasColumnType("citext").HasMaxLength(128).IsRequired();
            entity.Property(x => x.IsDeleted).IsRequired();
            entity.Property(x => x.IsSystem).IsRequired();
            entity.Property(x => x.AddedAtUtc)
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd()
                .IsRequired();
            entity.Property(x => x.RequestId).HasMaxLength(128).IsRequired(false);

            entity.HasIndex(x => x.UserId);

            // Уникальность активных имен (partial index)
            entity.HasIndex(x => new { x.UserId, x.Name })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_categories_user_name_active");

            // Идемпотентность создания категории только если RequestId задан
            entity.HasIndex(x => new { x.UserId, x.RequestId })
                .IsUnique()
                .HasFilter("\"RequestId\" IS NOT NULL")
                .HasDatabaseName("UX_categories_user_request");

            // Системные нельзя пометить удаленными
            entity.ToTable(tb =>
                tb.HasCheckConstraint("CK_categories_not_system_deleted", "NOT (\"IsSystem\" AND \"IsDeleted\")"));

            // Альтернативный ключ под композитный FK из expenses
            entity.HasAlternateKey(x => new { x.UserId, x.Id })
                .HasName("AK_categories_user_id_id");
        });

        // MassTransit Inbox/Outbox таблицы
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
