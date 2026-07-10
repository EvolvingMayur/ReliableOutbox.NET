namespace ReliableOutbox.Domain.Orders;

public sealed class Order
{
    private Order()
    {
    }

    public Order(
        Guid id,
        Guid transactionId,
        string customerName,
        DateTimeOffset createdOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);

        Id = id;
        TransactionId = transactionId;
        CustomerName = customerName;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid Id { get; private set; }

    public Guid TransactionId { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedOnUtc { get; private set; }
}
