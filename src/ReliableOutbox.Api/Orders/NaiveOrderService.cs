using ReliableOutbox.Domain.Auditing;
using ReliableOutbox.Domain.Orders;
using ReliableOutbox.Infrastructure.Persistence;

namespace ReliableOutbox.Api.Orders;

public sealed class NaiveOrderService(
    ReliableOutboxDbContext dbContext,
    IAuditScheduler auditScheduler)
{
    public async Task<Order> CreateAsync(
        string customerName,
        bool simulateCrashAfterCommit,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var order = new Order(
            id: Guid.NewGuid(),
            transactionId: Guid.NewGuid(),
            customerName: customerName,
            createdOnUtc: now);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // The business data is now permanent.
        // The audit work has not yet been scheduled.
        if (simulateCrashAfterCommit)
        {
            throw new SimulatedProcessCrashException();
        }

        await auditScheduler.ScheduleAsync(
            new AuditWorkItem(
                Id: Guid.NewGuid(),
                TransactionId: order.TransactionId,
                OrderId: order.Id,
                RequestedOnUtc: DateTimeOffset.UtcNow),
            cancellationToken);

        return order;
    }
}
