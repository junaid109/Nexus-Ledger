using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NexusLedger.Infrastructure.Data;
using NexusLedger.Infrastructure.Domain.Entities;
using NexusLedger.ReconciliationWorker.Application;
using NexusLedger.ReconciliationWorker.Infrastructure.BankSources;
using Xunit;

namespace NexusLedger.ReconciliationWorker.Tests;

public class ReconciliationServiceTests
{
    private readonly Mock<IBankSource> _bankSourceMock;
    private readonly Mock<ILogger<ReconciliationService>> _loggerMock;
    private readonly LedgerDbContext _dbContext;

    public ReconciliationServiceTests()
    {
        _bankSourceMock = new Mock<IBankSource>();
        _loggerMock = new Mock<ILogger<ReconciliationService>>();

        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new LedgerDbContext(options);
    }

    [Fact]
    public async Task ReconcileAsync_Should_RecordDiscrepancy_When_AmountMismatch()
    {
        // Arrange
        var txId = Guid.NewGuid();
        var date = DateTime.Today;

        _dbContext.LedgerEntries.Add(new LedgerEntry 
        { 
            Id = Guid.NewGuid(), 
            TransactionId = txId, 
            Amount = 100.00m, 
            Timestamp = date, 
            Type = LedgerEntryType.Credit 
        });
        await _dbContext.SaveChangesAsync();

        _bankSourceMock.Setup(b => b.GetTransactionsAsync(date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> 
            { 
                new BankTransaction(txId, 150.00m, "USD", date, "Test")
            });

        var service = new ReconciliationService(_loggerMock.Object, _dbContext, _bankSourceMock.Object);

        // Act
        await service.ReconcileAsync(date);

        // Assert
        var discrepancy = await _dbContext.Discrepancies.FirstOrDefaultAsync(d => d.TransactionId == txId);
        Assert.NotNull(discrepancy);
        Assert.Equal("Amount Mismatch", discrepancy.Reason);
        Assert.Equal(100.00m, discrepancy.AmountLedger);
        Assert.Equal(150.00m, discrepancy.AmountBank);
    }

    [Fact]
    public async Task ReconcileAsync_Should_RecordDiscrepancy_When_MissingInLedger()
    {
        // Arrange
        var txId = Guid.NewGuid();
        var date = DateTime.Today;

        _bankSourceMock.Setup(b => b.GetTransactionsAsync(date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> 
            { 
                new BankTransaction(txId, 150.00m, "USD", date, "Missing")
            });

        var service = new ReconciliationService(_loggerMock.Object, _dbContext, _bankSourceMock.Object);

        // Act
        await service.ReconcileAsync(date);

        // Assert
        var discrepancy = await _dbContext.Discrepancies.FirstOrDefaultAsync(d => d.TransactionId == txId);
        Assert.NotNull(discrepancy);
        Assert.Equal("Missing in Ledger", discrepancy.Reason);
    }

    [Fact]
    public async Task ReconcileAsync_Should_RecordDiscrepancy_When_MissingInBank()
    {
        // Arrange
        var txId = Guid.NewGuid();
        var date = DateTime.Today;

        _dbContext.LedgerEntries.Add(new LedgerEntry 
        { 
            Id = Guid.NewGuid(), 
            TransactionId = txId, 
            Amount = 100.00m, 
            Timestamp = date, 
            Type = LedgerEntryType.Credit 
        });
        await _dbContext.SaveChangesAsync();

        _bankSourceMock.Setup(b => b.GetTransactionsAsync(date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>());

        var service = new ReconciliationService(_loggerMock.Object, _dbContext, _bankSourceMock.Object);

        // Act
        await service.ReconcileAsync(date);

        // Assert
        var discrepancy = await _dbContext.Discrepancies.FirstOrDefaultAsync(d => d.TransactionId == txId);
        Assert.NotNull(discrepancy);
        Assert.Equal("Missing in Bank", discrepancy.Reason);
    }
}
