using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.Entities;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Queue.Infrastructure.Data;

namespace FNBReservation.Modules.Queue.Infrastructure.Repositories
{
    public class QueueRepository : IQueueRepository
    {
        private readonly QueueDbContext _dbContext;
        private readonly ILogger<QueueRepository> _logger;

        public QueueRepository(QueueDbContext dbContext, ILogger<QueueRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<QueueEntry> CreateAsync(QueueEntry queueEntry)
        {
            _logger.LogInformation("Creating new queue entry for {CustomerName} at outlet {OutletId}",
                queueEntry.CustomerName, queueEntry.OutletId);

            // Ensure queue position is set correctly
            int currentMaxPosition = await _dbContext.QueueEntries
                .Where(q => q.OutletId == queueEntry.OutletId &&
                           (q.Status == "Waiting" || q.Status == "Called" || q.Status == "Held"))
                .OrderByDescending(q => q.QueuePosition)
                .Select(q => q.QueuePosition)
                .FirstOrDefaultAsync();

            queueEntry.QueuePosition = currentMaxPosition + 1;
            queueEntry.CreatedAt = DateTime.UtcNow;
            queueEntry.UpdatedAt = DateTime.UtcNow;

            await _dbContext.QueueEntries.AddAsync(queueEntry);
            await _dbContext.SaveChangesAsync();

            return queueEntry;
        }

        public async Task<QueueEntry> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting queue entry by ID: {QueueEntryId}", id);

            return await _dbContext.QueueEntries
                .Include(q => q.TableAssignments)
                .Include(q => q.StatusChanges)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<QueueEntry> GetByCodeAsync(string queueCode)
        {
            _logger.LogInformation("Getting queue entry by code: {QueueCode}", queueCode);

            return await _dbContext.QueueEntries
                .Include(q => q.TableAssignments)
                .Include(q => q.StatusChanges)
                .FirstOrDefaultAsync(q => q.QueueCode == queueCode);
        }

        public async Task<IEnumerable<QueueEntry>> GetByOutletIdAsync(Guid outletId, string status = null)
        {
            _logger.LogInformation("Getting queue entries for outlet: {OutletId}, Status: {Status}",
                outletId, status);

            var query = _dbContext.QueueEntries
                .Include(q => q.TableAssignments)
                .Where(q => q.OutletId == outletId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(q => q.Status == status);
            }
            else
            {
                // If no specific status is requested, exclude DayEnd entries
                query = query.Where(q => q.Status != "DayEnd");
            }

            return await query
                .OrderBy(q => q.QueuePosition)
                .ToListAsync();
        }

        public async Task<QueueEntry> UpdateAsync(QueueEntry queueEntry)
        {
            _logger.LogInformation("Updating queue entry: {QueueEntryId}", queueEntry.Id);

            queueEntry.UpdatedAt = DateTime.UtcNow;
            _dbContext.QueueEntries.Update(queueEntry);
            await _dbContext.SaveChangesAsync();

            return queueEntry;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting queue entry: {QueueEntryId}", id);

            var queueEntry = await _dbContext.QueueEntries.FindAsync(id);
            if (queueEntry == null)
            {
                _logger.LogWarning("Queue entry not found for deletion: {QueueEntryId}", id);
                return false;
            }

            _dbContext.QueueEntries.Remove(queueEntry);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task AddStatusChangeAsync(QueueStatusChange statusChange)
        {
            _logger.LogInformation("Adding status change for queue entry: {QueueEntryId}, from {OldStatus} to {NewStatus}",
                statusChange.QueueEntryId, statusChange.OldStatus, statusChange.NewStatus);

            await _dbContext.QueueStatusChanges.AddAsync(statusChange);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddTableAssignmentAsync(QueueTableAssignment tableAssignment)
        {
            _logger.LogInformation("Adding table assignment for queue entry: {QueueEntryId}, table: {TableId}",
                tableAssignment.QueueEntryId, tableAssignment.TableId);

            await _dbContext.QueueTableAssignments.AddAsync(tableAssignment);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateTableAssignmentAsync(QueueTableAssignment tableAssignment)
        {
            _logger.LogInformation("Updating table assignment: {TableAssignmentId}", tableAssignment.Id);

            _dbContext.QueueTableAssignments.Update(tableAssignment);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddNotificationAsync(QueueNotification notification)
        {
            _logger.LogInformation("Adding notification for queue entry: {QueueEntryId}, type: {NotificationType}",
                notification.QueueEntryId, notification.NotificationType);

            await _dbContext.QueueNotifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateNotificationAsync(QueueNotification notification)
        {
            _logger.LogInformation("Updating notification: {NotificationId}", notification.Id);

            _dbContext.QueueNotifications.Update(notification);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<int> GetQueuePositionAsync(Guid outletId, Guid queueEntryId)
        {
            _logger.LogInformation("Getting queue position for entry: {QueueEntryId} at outlet: {OutletId}",
                queueEntryId, outletId);

            // Get the queue entry to check its position
            var queueEntry = await _dbContext.QueueEntries
                .FirstOrDefaultAsync(q => q.Id == queueEntryId && q.OutletId == outletId);

            if (queueEntry == null)
            {
                _logger.LogWarning("Queue entry not found for position check: {QueueEntryId}", queueEntryId);
                return -1; // Indicates not found
            }

            return queueEntry.QueuePosition;
        }

        public async Task<int> CountActiveQueueEntriesAsync(Guid outletId)
        {
            _logger.LogInformation("Counting active queue entries for outlet: {OutletId}", outletId);

            return await _dbContext.QueueEntries
                .CountAsync(q => q.OutletId == outletId &&
                               (q.Status == "Waiting" || q.Status == "Called" || q.Status == "Held"));
        }

        public async Task<int> CountQueueEntriesByStatusAsync(Guid outletId, string status)
        {
            _logger.LogInformation("Counting queue entries for outlet: {OutletId} with status: {Status}",
                outletId, status);

            return await _dbContext.QueueEntries
                .CountAsync(q => q.OutletId == outletId && q.Status == status);
        }

        public async Task<int> GetLongestWaitTimeAsync(Guid outletId)
        {
            _logger.LogInformation("Getting longest wait time for outlet: {OutletId}", outletId);

            var oldestEntry = await _dbContext.QueueEntries
                .Where(q => q.OutletId == outletId && q.Status == "Waiting")
                .OrderBy(q => q.QueuedAt)
                .FirstOrDefaultAsync();

            if (oldestEntry == null)
            {
                return 0;
            }

            var waitTime = (int)(DateTime.UtcNow - oldestEntry.QueuedAt).TotalMinutes;
            return waitTime > 0 ? waitTime : 0;
        }

        public async Task<int> GetAverageWaitTimeAsync(Guid outletId)
        {
            _logger.LogInformation("Getting average wait time for outlet: {OutletId}", outletId);

            // Calculate average wait time for entries that went from Waiting to Seated in the last 24 hours
            var entries = await _dbContext.QueueEntries
                .Where(q => q.OutletId == outletId && q.Status == "Completed" && q.SeatedAt.HasValue &&
                           q.CompletedAt.HasValue && q.CompletedAt.Value >= DateTime.UtcNow.AddHours(-24))
                .ToListAsync();

            if (!entries.Any())
            {
                return 0;
            }

            var totalWaitMinutes = entries.Sum(q => (q.SeatedAt.Value - q.QueuedAt).TotalMinutes);
            return (int)(totalWaitMinutes / entries.Count);
        }

        public async Task<IEnumerable<QueueEntry>> GetActiveQueueEntriesByPartySize(Guid outletId, int minPartySize, int maxPartySize)
        {
            _logger.LogInformation("Getting active queue entries with party size between {MinSize} and {MaxSize} for outlet: {OutletId}",
                minPartySize, maxPartySize, outletId);

            return await _dbContext.QueueEntries
                .Where(q => q.OutletId == outletId &&
                           (q.Status == "Waiting" || q.Status == "Held") &&
                           q.PartySize >= minPartySize && q.PartySize <= maxPartySize)
                .OrderBy(q => q.IsHeld) // Non-held entries first
                .ThenBy(q => q.QueuePosition)
                .ToListAsync();
        }

        public async Task<IEnumerable<QueueEntry>> GetHeldEntriesAsync(Guid outletId)
        {
            _logger.LogInformation("Getting held queue entries for outlet: {OutletId}", outletId);

            return await _dbContext.QueueEntries
                .Where(q => q.OutletId == outletId && q.Status == "Waiting" && q.IsHeld)
                .OrderBy(q => q.HeldSince)
                .ToListAsync();
        }

        public async Task<double> GetAverageSeatingDurationAsync(Guid outletId, int partySize)
        {
            _logger.LogInformation("Getting average seating duration for party size {PartySize} at outlet: {OutletId}",
                partySize, outletId);

            // Calculate for similar party sizes (+/- 2)
            var minPartySize = Math.Max(1, partySize - 2);
            var maxPartySize = partySize + 2;

            var entries = await _dbContext.QueueEntries
                .Where(q => q.OutletId == outletId &&
                           q.Status == "Completed" &&
                           q.SeatedAt.HasValue &&
                           q.CompletedAt.HasValue &&
                           q.PartySize >= minPartySize &&
                           q.PartySize <= maxPartySize)
                .ToListAsync();

            if (!entries.Any())
            {
                return 60; // Default to 60 minutes if no data
            }

            var totalDuration = entries.Sum(q => (q.CompletedAt.Value - q.SeatedAt.Value).TotalMinutes);
            return totalDuration / entries.Count;
        }

        public async Task<(List<QueueEntry> Entries, int TotalCount)> SearchEntriesAsync(
            Guid outletId,
            List<string> statuses,
            string searchTerm,
            int page,
            int pageSize)
        {
            _logger.LogInformation("Searching queue entries for outlet: {OutletId}", outletId);

            IQueryable<QueueEntry> query = _dbContext.QueueEntries
                .Include(q => q.TableAssignments)
                .Where(q => q.OutletId == outletId);

            // Apply status filter
            if (statuses != null && statuses.Any())
            {
                query = query.Where(q => statuses.Contains(q.Status));
            }

            // Apply search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchTermLower = searchTerm.ToLower();
                query = query.Where(q =>
                    q.CustomerName.ToLower().Contains(searchTermLower) ||
                    q.CustomerPhone.Contains(searchTermLower) ||
                    q.QueueCode.ToLower().Contains(searchTermLower));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var entries = await query
                .OrderBy(q => q.Status == "Waiting" ? 0 :
                         q.Status == "Called" ? 1 :
                         q.Status == "Seated" ? 2 : 3)
                .ThenBy(q => q.QueuePosition)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (entries, totalCount);
        }

        public async Task<IEnumerable<QueueTableAssignment>> GetActiveTableAssignmentsAsync(Guid tableId)
        {
            _logger.LogInformation("Checking active assignments for table: {TableId}", tableId);

            return await _dbContext.QueueTableAssignments
                .Include(ta => ta.QueueEntry)
                .Where(ta => ta.TableId == tableId &&
                            (ta.QueueEntry.Status == "Waiting" ||
                             ta.QueueEntry.Status == "Called" ||
                             ta.QueueEntry.Status == "Seated"))
                .ToListAsync();
        }
    }
}