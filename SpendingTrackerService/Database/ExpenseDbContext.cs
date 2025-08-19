using Microsoft.EntityFrameworkCore;

namespace SpendingTrackerService.Database;

internal class ExpenseDbContext : DbContext
{
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();

    public ExpenseDbContext(DbContextOptions<ExpenseDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExpenseEntity>(entity =>
        {
            entity.ToTable("expenses");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id);

            entity.Property(e => e.Category)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Amount)
                  .IsRequired()
                  .HasPrecision(18, 2);

            entity.Property(e => e.Comment)
                  .HasMaxLength(500);

            entity.Property(e => e.AddDate)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
