using MassTransit;
using Microsoft.EntityFrameworkCore;
using StatisticsService.Infrastructure.Persistence.Entities;

namespace StatisticsService.Infrastructure.Persistence;

internal sealed class ApplicationDbContext : DbContext
{
    public DbSet<StatsDailyEntity> StatsDaily => Set<StatsDailyEntity>();
    public DbSet<StatsDailyByCategoryEntity> StatsDailyByCategory => Set<StatsDailyByCategoryEntity>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatsDailyEntity>(entity =>
        {
            entity.ToTable("user_stats_daily");

            entity.HasKey(e => new { e.UserId, e.Day });

            entity.HasIndex(e => new { e.UserId, e.Day })
                .HasDatabaseName("ix_user_stats_daily_user_day_desc")
                .IsDescending();

            entity.HasIndex(e => e.Day)
                .HasDatabaseName("ix_user_stats_daily_day");

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Day)
                .HasColumnType("date");

            entity.Property(e => e.AmountTotal)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.MaxExpenseAmount)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.ExpensesCount)
                .IsRequired();

            entity.Property(e => e.MaxExpenseNote);

            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd()
                .IsRequired();
        });

        modelBuilder.Entity<StatsDailyByCategoryEntity>(entity =>
        {
            entity.ToTable("user_stats_daily_by_category");

            entity.HasKey(e => new { e.UserId, e.Day, e.CategoryId });

            entity.HasIndex(e => new { e.UserId, e.Day })
                .HasDatabaseName("ix_user_stats_dbc_user_day_desc")
                .IsDescending();

            entity.HasIndex(e => new { e.UserId, e.Day, e.AmountTotal })
                .HasDatabaseName("ix_user_stats_dbc_user_day_amount_desc")
                .IsDescending(false, true, true);

            entity.HasIndex(e => e.Day)
                .HasDatabaseName("ix_user_stats_dbc_day");

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Day)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(e => e.CategoryId)
                .IsRequired();

            entity.Property(e => e.CategoryName)
                .HasMaxLength(128);

            entity.Property(e => e.AmountTotal)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.ExpensesCount)
                .IsRequired();

            entity.Property(e => e.MaxExpenseAmount)
                .HasColumnType("numeric(19,2)")
                .IsRequired();

            entity.Property(e => e.MaxExpenseNote);

            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd()
                .IsRequired();
        });

        // MassTransit Inbox/Outbox таблицы
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
