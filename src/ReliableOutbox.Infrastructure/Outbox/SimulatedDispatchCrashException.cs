namespace ReliableOutbox.Infrastructure.Outbox;

public sealed class SimulatedDispatchCrashException()
    : Exception(
        "The process failed after external scheduling but before the outbox message was marked as processed.");
