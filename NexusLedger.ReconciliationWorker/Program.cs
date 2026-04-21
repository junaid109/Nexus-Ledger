using NexusLedger.Infrastructure.Data;
using NexusLedger.ReconciliationWorker;
using NexusLedger.ReconciliationWorker.Application;
using NexusLedger.ReconciliationWorker.Infrastructure.BankSources;
using NexusLedger.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<LedgerDbContext>("sqlserver", settings => 
{
    settings.DisableRetry = false;
});

builder.Services.AddSingleton<IBankSource>(sp => 
    new JsonBankSource("bank_records.json", sp.GetRequiredService<ILogger<JsonBankSource>>()));

builder.Services.AddScoped<ReconciliationService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
