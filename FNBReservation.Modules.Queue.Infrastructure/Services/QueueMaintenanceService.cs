using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.Entities;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Queue.Infrastructure.Data;

namespace FNBReservation.Modules.Queue.Infrastructure.Services
{
    public class QueueMaintenanceService : IQueueMaintenanceService
    {
        private readonly QueueDbContext _dbContext;
        private readonly ILogger<QueueMaintenanceService> _logger;

        public QueueMaintenanceService(
            QueueDbContext dbContext,
            ILogger<QueueMaintenanceService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CleanupActiveQueueEntriesAsync()
        {
            _logger.LogInformation("Starting end-of-day queue cleanup");

            try
            {
                // Get all active entries (Waiting, Called, Held)
                var activeEntries = await _dbContext.QueueEntries
                    .Where(q => q.Status == "Waiting" || q.Status == "Called" || q.Status == "Held")
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active entries to clean up", activeEntries.Count);

                if (activeEntries.Count == 0)
                {
                    _logger.LogInformation("No active entries to clean up, skipping");
                    return;
                }

                // Mark all with the DayEnd status
                foreach (var entry in activeEntries)
                {
                    var oldStatus = entry.Status;
                    entry.Status = "DayEnd"; // A new status specifically for end-of-day cleanup
                    entry.UpdatedAt = DateTime.UtcNow;
                    entry.QueuePosition = 0; // Reset position

                    // Add status change record
                    var statusChange = new QueueStatusChange
                    {
                        Id = Guid.NewGuid(),
                        QueueEntryId = entry.Id,
                        OldStatus = oldStatus,
                        NewStatus = "DayEnd",
                        ChangedAt = DateTime.UtcNow,
                        Reason = "System: End of business day cleanup"
                    };

                    await _dbContext.QueueStatusChanges.AddAsync(statusChange);
                }

                // Save changes
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Completed end-of-day queue cleanup. Processed {Count} entries", activeEntries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during end-of-day queue cleanup");
                throw;
            }
        }
    }
}