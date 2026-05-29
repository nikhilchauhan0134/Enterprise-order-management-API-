using Microsoft.EntityFrameworkCore;
using SOPS.Domain.Entities;

namespace SOPS.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OutboxOrder> OutboxOrders => Set<OutboxOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.ExternalId).IsUnique();
            e.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.ToTable("OrderLines");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order).WithMany(o => o.Lines).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OutboxOrder>(e =>
        {
            e.ToTable("OutboxOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Payload).HasColumnType("nvarchar(max)");
            e.Property(x => x.Status).HasMaxLength(50);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });
    }
}
