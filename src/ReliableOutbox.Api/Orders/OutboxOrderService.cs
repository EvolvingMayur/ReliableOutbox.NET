using System.Text.Json;
using ReliableOutbox.Domain.Auditing;
using ReliableOutbox.Domain.Orders;
using ReliableOutbox.Domain.Outbox;
using ReliableOutbox.Infrastructure.Persistence;

namespace ReliableOutbox.Api.Orders;

public sealed class OutboxOrderService(
    ReliableOutboxDbContext dbContext)
{
    public async Task<Order> CreateAsync(
        string customerName,
        bool simulateFailureBeforeCommit,
        bool simulateCrashAfterCommit,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var transactionId = Guid.NewGuid();

        var order = new Order(
            id: Guid.NewGuid(),
            transactionId: transactionId,
            customerName: customerName,
            createdOnUtc: now);

        var auditWorkItem = new AuditWorkItem(
            Id: Guid.NewGuid(),
            TransactionId: transactionId,
            OrderId: order.Id,
            RequestedOnUtc: now);

        var outboxMessage = new OutboxMessage(
            id: auditWorkItem.Id,
            transactionId: transactionId,
            messageType: "AuditWorkRequested",
            payload: JsonSerializer.Serialize(auditWorkItem),
            occurredOnUtc: now);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Orders.Add(order);
        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (simulateFailureBeforeCommit)
        {
            throw new SimulatedOutboxFailureException(
                "Failure occurred before the database transaction committed.");
        }

        await transaction.CommitAsync(cancellationToken);

        if (simulateCrashAfterCommit)
        {
            throw new SimulatedOutboxFailureException(
                "The process failed after commit. The outbox message remains durable.");
        }

        return order;
    }
}
