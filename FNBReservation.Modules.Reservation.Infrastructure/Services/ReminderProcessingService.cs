// FNBReservation.Modules.Reservation.Infrastructure/Services/ReminderProcessingService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.Interfaces;

namespace FNBReservation.Modules.Reservation.Infrastructure.Services
{
    public class ReminderProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderProcessingService> _logger;
        private readonly TimeSpan _processInterval;

        public ReminderProcessingService(
            IServiceProvider serviceProvider,
            ILogger<ReminderProcessingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _processInterval = TimeSpan.FromMinutes(1); // Check reminders every minute
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reminder Processing Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reminders");
                }

                await Task.Delay(_processInterval, stoppingToken);
            }

            _logger.LogInformation("Reminder Processing Service is stopping");
        }

        private async Task ProcessRemindersAsync()
        {
            _logger.LogDebug("Processing due reminders");

            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IReservationNotificationService>();

            try
            {
                await notificationService.ProcessPendingRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing reminders");
            }
        }
    }
}