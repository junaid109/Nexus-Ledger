namespace NexusLedger.PaymentGateway.Domain.Events;

public record PaymentInitiated
{
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string FromAccount { get; init; } = string.Empty;
    public string ToAccount { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
