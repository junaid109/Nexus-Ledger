using System.ComponentModel.DataAnnotations;

namespace NexusLedger.PaymentGateway.Domain.Models;

public record PaymentRequest(
    [Required] Guid FromAccount,
    [Required] Guid ToAccount,
    [Required] decimal Amount,
    [Required] string Currency
);

public record PaymentResponse(
    Guid TransactionId,
    string Status,
    DateTime Timestamp
);
