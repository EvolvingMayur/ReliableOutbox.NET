using Microsoft.EntityFrameworkCore;
using ReliableOutbox.Domain.Orders;
using ReliableOutbox.Domain.Outbox;

namespace ReliableOutbox.Infrastructure.Persistence;

public sealed class ReliableOutboxDbContext(
    DbContextOptions<ReliableOutboxDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureOrders(modelBuilder);
        ConfigureOutboxMessages(modelBuilder);
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
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

    private static void ConfigureOutboxMessages(
        ModelBuilder modelBuilder)
    {
        var message = modelBuilder.Entity<OutboxMessage>();

        message.ToTable("outbox_messages");

        message.HasKey(entity => entity.Id);

        message.Property(entity => entity.Id)
            .HasColumnName("id");

        message.Property(entity => entity.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        message.Property(entity => entity.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(500)
            .IsRequired();

        message.Property(entity => entity.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        message.Property(entity => entity.OccurredOnUtc)
            .HasColumnName("occurred_on_utc")
            .IsRequired();

        message.Property(entity => entity.ProcessedOnUtc)
            .HasColumnName("processed_on_utc");

        message.Property(entity => entity.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        message.Property(entity => entity.LastError)
            .HasColumnName("last_error");
    }
}
