namespace ReliableOutbox.Api.Orders;

public sealed class SimulatedOutboxFailureException(
    string message)
    : Exception(message);
