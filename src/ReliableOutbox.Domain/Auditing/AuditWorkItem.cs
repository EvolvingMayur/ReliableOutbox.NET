namespace ReliableOutbox.Domain.Auditing;

public sealed record AuditWorkItem(
    Guid Id,
    Guid TransactionId,
    Guid OrderId,
    DateTimeOffset RequestedOnUtc);
