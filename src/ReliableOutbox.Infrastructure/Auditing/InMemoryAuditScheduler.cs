using System.Collections.Concurrent;
using ReliableOutbox.Domain.Auditing;

namespace ReliableOutbox.Infrastructure.Auditing;

public sealed class InMemoryAuditScheduler : IAuditScheduler
{
    private readonly ConcurrentQueue<AuditWorkItem> _scheduledItems = new();

    public IReadOnlyCollection<AuditWorkItem> ScheduledItems =>
        _scheduledItems.ToArray();

    public Task ScheduleAsync(
        AuditWorkItem workItem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _scheduledItems.Enqueue(workItem);

        return Task.CompletedTask;
    }
}
