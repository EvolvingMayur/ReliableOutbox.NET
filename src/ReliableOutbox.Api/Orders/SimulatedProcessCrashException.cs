namespace ReliableOutbox.Api.Orders;

public sealed class SimulatedProcessCrashException()
    : Exception(
        "The process failed after the database commit but before audit scheduling.");
