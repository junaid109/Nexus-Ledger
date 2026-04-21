namespace NexusLedger.PaymentGateway.Domain.Events;

public record PaymentInitiatedEvent(
    Guid PaymentId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    DateTime CreatedAt
);
