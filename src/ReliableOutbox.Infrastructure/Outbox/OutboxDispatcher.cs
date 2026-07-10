using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReliableOutbox.Domain.Auditing;
using ReliableOutbox.Infrastructure.Persistence;

namespace ReliableOutbox.Infrastructure.Outbox;

public sealed class OutboxDispatcher(
    ReliableOutboxDbContext dbContext,
    IAuditScheduler auditScheduler)
{
    public async Task<int> DispatchPendingAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "Batch size must be greater than zero.");
        }

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var successfulDispatchCount = 0;

        foreach (var message in messages)
        {
            try
            {
                if (message.MessageType != "AuditWorkRequested")
                {
                    throw new InvalidOperationException(
                        $"Unsupported message type: {message.MessageType}");
                }

                var workItem =
                    JsonSerializer.Deserialize<AuditWorkItem>(
                        message.Payload)
                    ?? throw new InvalidOperationException(
                        "The outbox payload could not be deserialized.");

                await auditScheduler.ScheduleAsync(
                    workItem,
                    cancellationToken);

                message.MarkProcessed(DateTimeOffset.UtcNow);

                successfulDispatchCount++;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.MarkFailed(exception.Message);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return successfulDispatchCount;
    }
}
