using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.Interfaces;

namespace FNBReservation.Modules.Queue.Infrastructure.Services
{
    public class QueueCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueueCleanupBackgroundService> _logger;

        public QueueCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<QueueCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue cleanup background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate time until next run (2 AM)
                var now = DateTime.Now;
                var nextRun = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0);
                if (now > nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                _logger.LogInformation("Next queue cleanup scheduled for: {NextRun}, in {Delay}",
                    nextRun, delay);

                // Wait until the next scheduled run time
                await Task.Delay(delay, stoppingToken);

                // Execute the cleanup
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var maintenanceService = scope.ServiceProvider.GetRequiredService<IQueueMaintenanceService>();
                    await maintenanceService.CleanupActiveQueueEntriesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running daily queue cleanup");
                }

                // Add a small delay to prevent tight loop if there's an issue with time calculation
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}