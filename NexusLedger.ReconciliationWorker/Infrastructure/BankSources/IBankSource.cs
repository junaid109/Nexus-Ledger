namespace NexusLedger.ReconciliationWorker.Infrastructure.BankSources;

public record BankTransaction(
    Guid TransactionId,
    decimal Amount,
    string Currency,
    DateTime Timestamp,
    string Reference
);

public interface IBankSource
{
    Task<IEnumerable<BankTransaction>> GetTransactionsAsync(DateTime date, CancellationToken ct = default);
}
