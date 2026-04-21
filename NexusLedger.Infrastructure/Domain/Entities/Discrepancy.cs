namespace NexusLedger.Infrastructure.Domain.Entities;

public class Discrepancy
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public decimal AmountLedger { get; set; }
    public decimal AmountBank { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
}
