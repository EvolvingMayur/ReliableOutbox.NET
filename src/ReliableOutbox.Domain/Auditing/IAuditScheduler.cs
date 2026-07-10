namespace ReliableOutbox.Domain.Auditing;

public interface IAuditScheduler
{
    Task ScheduleAsync(
        AuditWorkItem workItem,
        CancellationToken cancellationToken);
}
