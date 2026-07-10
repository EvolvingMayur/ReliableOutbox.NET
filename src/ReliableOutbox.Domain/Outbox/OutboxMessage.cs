namespace ReliableOutbox.Domain.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        Guid transactionId,
        string messageType,
        string payload,
        DateTimeOffset occurredOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        Id = id;
        TransactionId = transactionId;
        MessageType = messageType;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid Id { get; private set; }

    public Guid TransactionId { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredOnUtc { get; private set; }

    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public string? LastError { get; private set; }

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        AttemptCount++;
        ProcessedOnUtc = processedOnUtc;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        AttemptCount++;
        LastError = error;
    }
}
