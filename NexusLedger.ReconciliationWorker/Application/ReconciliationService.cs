using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusLedger.Infrastructure.Data;
using NexusLedger.Infrastructure.Domain.Entities;
using NexusLedger.ReconciliationWorker.Infrastructure.BankSources;

namespace NexusLedger.ReconciliationWorker.Application;

public class ReconciliationService
{
    private readonly ILogger<ReconciliationService> _logger;
    private readonly LedgerDbContext _dbContext;
    private readonly IBankSource _bankSource;

    public ReconciliationService(
        ILogger<ReconciliationService> logger, 
        LedgerDbContext dbContext, 
        IBankSource bankSource)
    {
        _logger = logger;
        _dbContext = dbContext;
        _bankSource = bankSource;
    }

    public async Task ReconcileAsync(DateTime date, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting reconciliation for {Date}", date.ToShortDateString());

        var bankTransactions = (await _bankSource.GetTransactionsAsync(date, ct)).ToList();
        var ledgerEntries = await _dbContext.LedgerEntries
            .Where(l => l.Timestamp.Date == date.Date && l.Type == LedgerEntryType.Credit) // Simple comparison: match bank credits vs our ledger credits
            .ToListAsync(ct);

        _logger.LogInformation("Found {BankCount} bank records and {LedgerCount} ledger entries.", bankTransactions.Count, ledgerEntries.Count);

        foreach (var bankTx in bankTransactions)
        {
            var ledgerEntry = ledgerEntries.FirstOrDefault(l => l.TransactionId == bankTx.TransactionId);

            if (ledgerEntry == null)
            {
                await RecordDiscrepancy(bankTx.TransactionId, 0, bankTx.Amount, "Missing in Ledger", ct);
                continue;
            }

            if (ledgerEntry.Amount != bankTx.Amount)
            {
                await RecordDiscrepancy(bankTx.TransactionId, ledgerEntry.Amount, bankTx.Amount, "Amount Mismatch", ct);
            }
            else
            {
                _logger.LogInformation("Match found for transaction {TransactionId}", bankTx.TransactionId);
            }
        }

        // Check for ledger entries missing in bank
        foreach (var ledgerEntry in ledgerEntries)
        {
            if (!bankTransactions.Any(b => b.TransactionId == ledgerEntry.TransactionId))
            {
                await RecordDiscrepancy(ledgerEntry.TransactionId, ledgerEntry.Amount, 0, "Missing in Bank", ct);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Reconciliation completed for {Date}", date.ToShortDateString());
    }

    private async Task RecordDiscrepancy(Guid txId, decimal ledgerAmount, decimal bankAmount, string reason, CancellationToken ct)
    {
        _logger.LogWarning("Discrepancy detected: {TxId} - {Reason}. Ledger: {LedgerAmt}, Bank: {BankAmt}", 
            txId, reason, ledgerAmount, bankAmount);

        var existing = await _dbContext.Discrepancies.AnyAsync(d => d.TransactionId == txId, ct);
        if (existing) return;

        var discrepancy = new Discrepancy
        {
            Id = Guid.NewGuid(),
            TransactionId = txId,
            AmountLedger = ledgerAmount,
            AmountBank = bankAmount,
            Reason = reason,
            DetectedAt = DateTime.UtcNow,
            IsResolved = false
        };

        _dbContext.Discrepancies.Add(discrepancy);
    }
}
