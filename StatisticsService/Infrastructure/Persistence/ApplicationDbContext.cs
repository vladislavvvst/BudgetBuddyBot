using MassTransit;
using Microsoft.EntityFrameworkCore;
using StatisticsService.Infrastructure.Persistence.Entities;

namespace StatisticsService.Infrastructure.Persistence;

internal sealed class ApplicationDbContext : DbContext
{
    public DbSet<StatsDailyEntity> StatsDaily => Set<StatsDailyEntity>();
    public DbSet<StatsDailyByCategoryEntity> StatsDailyByCategory => Set<StatsDailyByCategoryEntity>();
    public DbSet<ProcessedExpenseRequestEntity> ProcessedExpenseRequests => Set<ProcessedExpenseRequestEntity>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatsDailyEntity>(entity =>
        {
            entity.ToTable("user_stats_daily");

            entity.HasKey(e => new { e.UserId, e.Day });

            // Основной индекс для запросов по пользователю и диапазону дат
            entity.HasIndex(e => new { e.UserId, e.Day })
                .HasDatabaseName("ix_user_stats_daily_user_day")
                .IsDescending(false, true); // UserId ASC, Day DESC

            entity.HasIndex(e => e.Day)
                .HasDatabaseName("ix_user_stats_daily_day");

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Day).HasColumnType("date");

            entity.Property(e => e.AmountTotal)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.MaxExpenseAmount)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.ExpensesCount).IsRequired();
            entity.Property(e => e.MaxExpenseNote);

            entity.Property(e => e.UpdatedAtUtc)
                .IsRequired(); // Поставить руками значение!
        });

        modelBuilder.Entity<StatsDailyByCategoryEntity>(entity =>
        {
            entity.ToTable("user_stats_daily_by_category");

            entity.HasKey(e => new { e.UserId, e.Day, e.CategoryId });

            // Основные запросы по пользователю + диапазон дат
            entity.HasIndex(e => new { e.UserId, e.Day })
                .HasDatabaseName("ix_user_stats_dbc_user_day")
                .IsDescending(false, true); // UserId ASC, Day DESC

            // Топ категорий по сумме за день для пользователя
            entity.HasIndex(e => new { e.UserId, e.Day, e.AmountTotal })
                .HasDatabaseName("ix_user_stats_dbc_user_day_amount")
                .IsDescending(false, true, true); // UserId ASC, Day DESC, AmountTotal DESC

            entity.HasIndex(e => e.Day)
                .HasDatabaseName("ix_user_stats_dbc_day");

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Day).HasColumnType("date").IsRequired();
            entity.Property(e => e.CategoryId).IsRequired();

            entity.Property(e => e.CategoryName)
                .HasMaxLength(128);

            entity.Property(e => e.AmountTotal)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.ExpensesCount).IsRequired();

            entity.Property(e => e.MaxExpenseAmount)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.MaxExpenseNote);

            entity.Property(e => e.UpdatedAtUtc)
                .IsRequired(); // Поставить руками значение!
        });

        modelBuilder.Entity<ProcessedExpenseRequestEntity>(entity =>
        {
            entity.ToTable("processed_expense_requests");
            entity.HasKey(e => new { e.UserId, e.RequestId });

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RequestId)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(e => e.ProcessedAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(e => e.ProcessedAtUtc)
                .HasDatabaseName("IX_processed_expense_requests_processed_at");
        });

        // MassTransit Inbox/Outbox
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
