namespace NexusLedger.Infrastructure.Domain.Entities;

public enum LedgerEntryType
{
    Debit,
    Credit
}

public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public LedgerEntryType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string Currency { get; set; } = string.Empty;
}
