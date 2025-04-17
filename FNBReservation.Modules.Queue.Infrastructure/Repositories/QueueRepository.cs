using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.Entities;
using FNBReservation.Modules.Queue.Core.Interfaces;
using FNBReservation.Modules.Queue.Infrastructure.Data;
using FNBReservation.SharedKernel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FNBReservation.Modules.Queue.Infrastructure.Repositories
{
    public class QueueRepository : BaseRepository<QueueEntry, QueueDbContext>, IQueueRepository
    {
        private readonly ILogger<QueueRepository> _logger;

        public QueueRepository(
            DbContextFactory<QueueDbContext> contextFactory,
            ILogger<QueueRepository> logger)
            : base(contextFactory, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<QueueEntry> CreateAsync(QueueEntry queueEntry)
        {
            _logger.LogInformation("Creating new queue entry for {CustomerName} at outlet {OutletId}",
                queueEntry.CustomerName, queueEntry.OutletId);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Ensure queue position is set correctly
                int currentMaxPosition = await context.QueueEntries
                    .Where(q => q.OutletId == queueEntry.OutletId &&
                               (q.Status == "Waiting" || q.Status == "Called" || q.Status == "Held"))
                    .OrderByDescending(q => q.QueuePosition)
                    .Select(q => q.QueuePosition)
                    .FirstOrDefaultAsync();

                queueEntry.QueuePosition = currentMaxPosition + 1;
                queueEntry.CreatedAt = DateTime.UtcNow;
                queueEntry.UpdatedAt = DateTime.UtcNow;

                await context.QueueEntries.AddAsync(queueEntry);
                await context.SaveChangesAsync();

                return queueEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating queue entry for {CustomerName}", queueEntry.CustomerName);
                throw;
            }
        }

        public async Task<QueueEntry> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting queue entry by ID: {QueueEntryId}", id);

            using var context = _contextFactory.CreateReadContext();
            return await context.QueueEntries
                .Include(q => q.TableAssignments)
                .Include(q => q.StatusChanges)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<QueueEntry> GetByCodeAsync(string queueCode)
        {
            _logger.LogInformation("Getting queue entry by code: {QueueCode}", queueCode);

            using var context = _contextFactory.CreateReadContext();
            return await context.QueueEntries
                .Include(q => q.TableAssignments)
                .Include(q => q.StatusChanges)
                .FirstOrDefaultAsync(q => q.QueueCode == queueCode);
        }

        public async Task<IEnumerable<QueueEntry>> GetByOutletIdAsync(Guid outletId, string status = null)
        {
            _logger.LogInformation("Getting queue entries for outlet: {OutletId}, Status: {Status}",
                outletId, status);

            using var context = _contextFactory.CreateReadContext();
            var query = context.QueueEntries
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

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Retrieve the entry with its related entities for tracking
                var existingEntry = await context.QueueEntries
                    .Include(q => q.TableAssignments)
                    .Include(q => q.StatusChanges)
                    .FirstOrDefaultAsync(q => q.Id == queueEntry.Id);

                if (existingEntry == null)
                {
                    _logger.LogWarning("Queue entry not found for update: {QueueEntryId}", queueEntry.Id);
                    throw new KeyNotFoundException($"Queue entry with ID {queueEntry.Id} not found");
                }

                // Update properties
                context.Entry(existingEntry).CurrentValues.SetValues(queueEntry);
                existingEntry.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await context.SaveChangesAsync();

                return existingEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue entry: {QueueEntryId}", queueEntry.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting queue entry: {QueueEntryId}", id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Use FindAsync for primary key lookup for efficiency
                var queueEntry = await context.QueueEntries.FindAsync(id);
                if (queueEntry == null)
                {
                    _logger.LogWarning("Queue entry not found for deletion: {QueueEntryId}", id);
                    return false;
                }

                // Load related entities before deletion
                await context.Entry(queueEntry)
                    .Collection(q => q.TableAssignments)
                    .LoadAsync();

                await context.Entry(queueEntry)
                    .Collection(q => q.StatusChanges)
                    .LoadAsync();

                // Before deleting, check if there are related entries that need to be handled
                var tableAssignments = queueEntry.TableAssignments.ToList();
                var statusChanges = queueEntry.StatusChanges.ToList();
                var notifications = await context.QueueNotifications
                    .Where(n => n.QueueEntryId == id)
                    .ToListAsync();

                // Remove related entities if they exist
                if (notifications.Any())
                {
                    context.QueueNotifications.RemoveRange(notifications);
                }

                if (statusChanges.Any())
                {
                    context.QueueStatusChanges.RemoveRange(statusChanges);
                }

                if (tableAssignments.Any())
                {
                    context.QueueTableAssignments.RemoveRange(tableAssignments);
                }

                // Now remove the queue entry
                context.QueueEntries.Remove(queueEntry);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting queue entry: {QueueEntryId}", id);
                throw;
            }
        }

        public async Task AddStatusChangeAsync(QueueStatusChange statusChange)
        {
            _logger.LogInformation("Adding status change for queue entry: {QueueEntryId}, from {OldStatus} to {NewStatus}",
                statusChange.QueueEntryId, statusChange.OldStatus, statusChange.NewStatus);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.QueueStatusChanges.AddAsync(statusChange);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding status change for queue entry: {QueueEntryId}",
                    statusChange.QueueEntryId);
                throw;
            }
        }

        public async Task AddTableAssignmentAsync(QueueTableAssignment tableAssignment)
        {
            _logger.LogInformation("Adding table assignment for queue entry: {QueueEntryId}, table: {TableId}",
                tableAssignment.QueueEntryId, tableAssignment.TableId);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.QueueTableAssignments.AddAsync(tableAssignment);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding table assignment for queue entry: {QueueEntryId}",
                    tableAssignment.QueueEntryId);
                throw;
            }
        }

        public async Task UpdateTableAssignmentAsync(QueueTableAssignment tableAssignment)
        {
            _logger.LogInformation("Updating table assignment: {TableAssignmentId}", tableAssignment.Id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Fetch the existing assignment to ensure proper tracking
                var existingAssignment = await context.QueueTableAssignments.FindAsync(tableAssignment.Id);
                if (existingAssignment == null)
                {
                    _logger.LogWarning("Table assignment not found: {TableAssignmentId}", tableAssignment.Id);
                    throw new KeyNotFoundException($"Table assignment with ID {tableAssignment.Id} not found");
                }

                // Update properties
                context.Entry(existingAssignment).CurrentValues.SetValues(tableAssignment);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating table assignment: {TableAssignmentId}",
                    tableAssignment.Id);
                throw;
            }
        }

        public async Task AddNotificationAsync(QueueNotification notification)
        {
            _logger.LogInformation("Adding notification for queue entry: {QueueEntryId}, type: {NotificationType}",
                notification.QueueEntryId, notification.NotificationType);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.QueueNotifications.AddAsync(notification);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notification for queue entry: {QueueEntryId}",
                    notification.QueueEntryId);
                throw;
            }
        }

        public async Task UpdateNotificationAsync(QueueNotification notification)
        {
            _logger.LogInformation("Updating notification: {NotificationId}", notification.Id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Fetch the existing notification to ensure proper tracking
                var existingNotification = await context.QueueNotifications.FindAsync(notification.Id);
                if (existingNotification == null)
                {
                    _logger.LogWarning("Notification not found: {NotificationId}", notification.Id);
                    throw new KeyNotFoundException($"Notification with ID {notification.Id} not found");
                }

                // Update properties
                context.Entry(existingNotification).CurrentValues.SetValues(notification);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification: {NotificationId}", notification.Id);
                throw;
            }
        }

        public async Task<int> GetQueuePositionAsync(Guid outletId, Guid queueEntryId)
        {
            _logger.LogInformation("Getting queue position for entry: {QueueEntryId} at outlet: {OutletId}",
                queueEntryId, outletId);

            using var context = _contextFactory.CreateReadContext();
            // Get the queue entry to check its position
            var queueEntry = await context.QueueEntries
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

            using var context = _contextFactory.CreateReadContext();
            return await context.QueueEntries
                .CountAsync(q => q.OutletId == outletId &&
                               (q.Status == "Waiting" || q.Status == "Called" || q.Status == "Held"));
        }

        public async Task<int> CountQueueEntriesByStatusAsync(Guid outletId, string status)
        {
            _logger.LogInformation("Counting queue entries for outlet: {OutletId} with status: {Status}",
                outletId, status);

            using var context = _contextFactory.CreateReadContext();
            return await context.QueueEntries
                .CountAsync(q => q.OutletId == outletId && q.Status == status);
        }

        public async Task<int> GetLongestWaitTimeAsync(Guid outletId)
        {
            _logger.LogInformation("Getting longest wait time for outlet: {OutletId}", outletId);

            using var context = _contextFactory.CreateReadContext();
            var oldestEntry = await context.QueueEntries
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

            using var context = _contextFactory.CreateReadContext();
            // Calculate average wait time for entries that went from Waiting to Seated in the last 24 hours
            var entries = await context.QueueEntries
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

            using var context = _contextFactory.CreateReadContext();
            return await context.QueueEntries
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

            using var context = _contextFactory.CreateReadContext();
            return await context.QueueEntries
                .Where(q => q.OutletId == outletId && q.Status == "Waiting" && q.IsHeld)
                .OrderBy(q => q.HeldSince)
                .ToListAsync();
        }

        public async Task<double> GetAverageSeatingDurationAsync(Guid outletId, int partySize)
        {
            _logger.LogInformation("Getting average seating duration for party size {PartySize} at outlet: {OutletId}",
                partySize, outletId);

            using var context = _contextFactory.CreateReadContext();
            // Calculate for similar party sizes (+/- 2)
            var minPartySize = Math.Max(1, partySize - 2);
            var maxPartySize = partySize + 2;

            var entries = await context.QueueEntries
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

            using var context = _contextFactory.CreateReadContext();
            IQueryable<QueueEntry> query = context.QueueEntries
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

            using var context = _contextFactory.CreateReadContext();
            return await context.QueueTableAssignments
                .Include(ta => ta.QueueEntry)
                .Where(ta => ta.TableId == tableId &&
                            (ta.QueueEntry.Status == "Waiting" ||
                             ta.QueueEntry.Status == "Called" ||
                             ta.QueueEntry.Status == "Seated"))
                .ToListAsync();
        }
    }
}