using Microsoft.EntityFrameworkCore;
using ReliableOutbox.Domain.Orders;

namespace ReliableOutbox.Infrastructure.Persistence;

public sealed class ReliableOutboxDbContext(
    DbContextOptions<ReliableOutboxDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();

        order.ToTable("orders");

        order.HasKey(entity => entity.Id);

        order.Property(entity => entity.Id)
            .HasColumnName("id");

        order.Property(entity => entity.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        order.Property(entity => entity.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(200)
            .IsRequired();

        order.Property(entity => entity.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();
    }
}
