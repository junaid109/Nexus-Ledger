namespace NexusLedger.SettlementService.App;

using Confluent.Kafka;
using NexusLedger.Infrastructure.Domain.Entities;
using NexusLedger.SettlementService.Domain.Events;
using NexusLedger.Infrastructure.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IConsumer<string, string> consumer, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _consumer = consumer;
        _serviceProvider = serviceProvider;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.Subscribe("payments-topic");
        
        // Ensure DB is created
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result != null)
                {
                    await ProcessPaymentAsync(result.Message.Value, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka message");
            }
        }
    }

    private async Task ProcessPaymentAsync(string messageValue, CancellationToken cancellationToken)
    {
        try 
        {
            var paymentEvent = JsonSerializer.Deserialize<PaymentInitiated>(messageValue);
            if (paymentEvent is null) return;

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();

            // Idempotency Check
            var existing = await context.LedgerEntries.AnyAsync(l => l.TransactionId == paymentEvent.TransactionId, cancellationToken);
            if (existing)
            {
                _logger.LogInformation("Payment {TransactionId} already settled. Skipping.", paymentEvent.TransactionId);
                return;
            }

            // Debit Sender
            var debitEntry = new LedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = paymentEvent.TransactionId,
                AccountId = paymentEvent.FromAccount,
                Amount = -paymentEvent.Amount, // Negative for debit
                Type = LedgerEntryType.Debit,
                Currency = paymentEvent.Currency,
                Timestamp = DateTime.UtcNow
            };

            // Credit Receiver
            var creditEntry = new LedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = paymentEvent.TransactionId,
                AccountId = paymentEvent.ToAccount,
                Amount = paymentEvent.Amount, // Positive for credit
                Type = LedgerEntryType.Credit,
                Currency = paymentEvent.Currency,
                Timestamp = DateTime.UtcNow
            };

            context.LedgerEntries.AddRange(debitEntry, creditEntry);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Settled payment {TransactionId}: {Amount} {Currency} from {From} to {To}", 
                paymentEvent.TransactionId, paymentEvent.Amount, paymentEvent.Currency, paymentEvent.FromAccount, paymentEvent.ToAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment event");
            throw; // simpler to throw and let consumer loop retry (if configured) or just log for now
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
