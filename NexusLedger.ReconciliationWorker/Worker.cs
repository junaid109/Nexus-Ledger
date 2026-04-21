using NexusLedger.ReconciliationWorker.Application;

namespace NexusLedger.ReconciliationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reconciliation Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var reconciliationService = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
                    
                    // In a real system, this might run once a day for "yesterday's" date.
                    // For this demo, we run it for "today" every minute.
                    await reconciliationService.ReconcileAsync(DateTime.UtcNow, stoppingToken);
                }

                _logger.LogInformation("Reconciliation job completed. Waiting for next cycle...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during reconciliation process.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
