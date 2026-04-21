using Microsoft.EntityFrameworkCore;
using NexusLedger.Infrastructure.Domain.Entities;

namespace NexusLedger.Infrastructure.Data;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options)
    {
    }

    public DbSet<LedgerEntry> LedgerEntries { get; set; }
    public DbSet<Discrepancy> Discrepancies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AccountId).IsRequired();
        });

        modelBuilder.Entity<Discrepancy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId);
            entity.Property(e => e.AmountLedger).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountBank).HasColumnType("decimal(18,2)");
        });
    }
}


