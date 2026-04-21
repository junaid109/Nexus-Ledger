using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NexusLedger.ReconciliationWorker.Infrastructure.BankSources;

public class JsonBankSource : IBankSource
{
    private readonly string _filePath;
    private readonly ILogger<JsonBankSource> _logger;

    public JsonBankSource(string filePath, ILogger<JsonBankSource> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    public async Task<IEnumerable<BankTransaction>> GetTransactionsAsync(DateTime date, CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("Bank records file not found at {FilePath}", _filePath);
            return Enumerable.Empty<BankTransaction>();
        }

        try
        {
            using var stream = File.OpenRead(_filePath);
            var transactions = await JsonSerializer.DeserializeAsync<List<BankTransaction>>(stream, cancellationToken: ct);
            
            // Filter by date (ignoring time for reconciliation)
            return transactions?
                .Where(t => t.Timestamp.Date == date.Date) 
                ?? Enumerable.Empty<BankTransaction>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading bank records from {FilePath}", _filePath);
            return Enumerable.Empty<BankTransaction>();
        }
    }
}
