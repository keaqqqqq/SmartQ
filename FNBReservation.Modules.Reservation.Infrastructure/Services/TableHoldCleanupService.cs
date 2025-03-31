// In FNBReservation.Modules.Reservation.Infrastructure/Services/TableHoldCleanupService.cs
using FNBReservation.Modules.Reservation.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
public class TableHoldCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TableHoldCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public TableHoldCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TableHoldCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Table Hold Cleanup Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessExpiredHoldsAsync();
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Table Hold Cleanup Service is stopping");
    }

    private async Task ProcessExpiredHoldsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();

            var expiredHolds = await repository.GetExpiredTableHoldsAsync();

            foreach (var hold in expiredHolds)
            {
                await repository.ReleaseTableHoldAsync(hold.Id);
                _logger.LogInformation("Released expired hold {HoldId} for tables at {DateTime}",
                    hold.Id, hold.ReservationDateTime);
            }

            if (expiredHolds.Any())
            {
                _logger.LogInformation("Released {Count} expired table holds", expiredHolds.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired table holds");
        }
    }
}