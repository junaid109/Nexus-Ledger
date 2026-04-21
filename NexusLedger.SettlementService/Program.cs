using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using NexusLedger.ServiceDefaults;
using NexusLedger.SettlementService.App;
using NexusLedger.Infrastructure.Data;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<LedgerDbContext>("sqlserver", settings => 
{
    // Enable retry on failure for SQL Server
    settings.DisableRetry = false; 
});
builder.AddKafkaConsumer<string, string>("kafka", settings => 
{
    settings.Config.GroupId = "settlement-group";
    settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
