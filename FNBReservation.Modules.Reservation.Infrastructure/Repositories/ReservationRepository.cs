using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.Entities;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Reservation.Infrastructure.Data;
using FNBReservation.SharedKernel.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace FNBReservation.Modules.Reservation.Infrastructure.Repositories
{
    public class ReservationRepository : BaseRepository<ReservationEntity, ReservationDbContext>, IReservationRepository
    {
        private readonly ILogger<ReservationRepository> _logger;
        private readonly IOutletAdapter _outletAdapter;

        public ReservationRepository(
            DbContextFactory<ReservationDbContext> contextFactory,
            ILogger<ReservationRepository> logger,
            IOutletAdapter outletAdapter)
            : base(contextFactory, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _outletAdapter = outletAdapter ?? throw new ArgumentNullException(nameof(outletAdapter));
        }

        public async Task<ReservationEntity> CreateAsync(ReservationEntity reservation)
        {
            _logger.LogInformation("Creating new reservation for {CustomerName} on {ReservationDate}",
                reservation.CustomerName, reservation.ReservationDate);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.Reservations.AddAsync(reservation);
                await context.SaveChangesAsync();
                return reservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation for {CustomerName}", reservation.CustomerName);
                throw;
            }
        }

        public async Task<ReservationEntity> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting reservation by ID: {ReservationId}", id);

            using var context = _contextFactory.CreateReadContext();
            return await context.Reservations
                .Include(r => r.TableAssignments)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<ReservationEntity> GetByCodeAsync(string reservationCode)
        {
            _logger.LogInformation("Getting reservation by code: {ReservationCode}", reservationCode);

            using var context = _contextFactory.CreateReadContext();
            return await context.Reservations
                .Include(r => r.TableAssignments)
                .FirstOrDefaultAsync(r => r.ReservationCode == reservationCode);
        }

        public async Task<IEnumerable<ReservationEntity>> GetByOutletIdAsync(Guid outletId, DateTime? date = null, string status = null)
        {
            _logger.LogInformation("Getting reservations for outlet: {OutletId}, Date: {Date}, Status: {Status}",
                outletId, date, status);

            using var context = _contextFactory.CreateReadContext();
            var query = context.Reservations
                .Include(r => r.TableAssignments)
                .Where(r => r.OutletId == outletId);

            if (date.HasValue)
            {
                DateTime startDate = date.Value.Date;
                DateTime endDate = startDate.AddDays(1);
                query = query.Where(r => r.ReservationDate >= startDate && r.ReservationDate < endDate);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReservationEntity>> GetByPhoneAsync(string phone)
        {
            _logger.LogInformation("Getting reservations for phone: {Phone}", phone);

            using var context = _contextFactory.CreateReadContext();
            return await context.Reservations
                .Include(r => r.TableAssignments)
                .Where(r => r.CustomerPhone == phone)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReservationEntity>> GetByDateRangeAsync(Guid outletId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Getting reservations for outlet: {OutletId} between {StartDate} and {EndDate}",
                outletId, startDate, endDate);

            using var context = _contextFactory.CreateReadContext();
            return await context.Reservations
                .Include(r => r.TableAssignments)
                .Where(r => r.OutletId == outletId &&
                            r.ReservationDate >= startDate &&
                            r.ReservationDate <= endDate)
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<ReservationEntity> UpdateAsync(ReservationEntity reservation)
        {
            _logger.LogInformation("Updating reservation: {ReservationId}", reservation.Id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                // Retrieve the entity with its related entities for tracking
                var existingReservation = await context.Reservations
                    .Include(r => r.TableAssignments)
                    .FirstOrDefaultAsync(r => r.Id == reservation.Id);

                if (existingReservation == null)
                {
                    _logger.LogWarning("Reservation not found for update: {ReservationId}", reservation.Id);
                    throw new KeyNotFoundException($"Reservation with ID {reservation.Id} not found");
                }

                // Update properties
                context.Entry(existingReservation).CurrentValues.SetValues(reservation);

                // Save changes
                await context.SaveChangesAsync();
                return existingReservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation: {ReservationId}", reservation.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting reservation: {ReservationId}", id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                var reservation = await context.Reservations.FindAsync(id);
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation not found for deletion: {ReservationId}", id);
                    return false;
                }

                // Load related entities before deletion
                await context.Entry(reservation)
                    .Collection(r => r.TableAssignments)
                    .LoadAsync();

                context.Reservations.Remove(reservation);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reservation: {ReservationId}", id);
                throw;
            }
        }

        public async Task<int> GetReservationCountForTimeSlotAsync(Guid outletId, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Getting reservation count for outlet: {OutletId} between {StartTime} and {EndTime}",
                outletId, startTime, endTime);

            using var context = _contextFactory.CreateReadContext();
            // First, get the reservations that might overlap
            var overlappingReservations = await context.Reservations
                .Where(r => r.OutletId == outletId &&
                           r.Status != "Canceled" &&
                           r.Status != "NoShow" &&
                           r.ReservationDate < endTime)
                .ToListAsync();

            // Then filter and count client-side
            return overlappingReservations
                .Count(r => r.ReservationDate.Add(r.Duration) > startTime);
        }

        // Updated GetReservedCapacityForTimeSlotAsync method
        public async Task<int> GetReservedCapacityForTimeSlotAsync(Guid outletId, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Getting reserved capacity for outlet: {OutletId} between {StartTime} and {EndTime}",
                outletId, startTime, endTime);

            using var context = _contextFactory.CreateReadContext();
            try
            {
                // Remove the explicit transaction since it's causing issues with MySqlRetryingExecutionStrategy
                // Get all active reservations for this time slot with a more accurate overlap check
                var overlappingReservations = await context.Reservations
                    .Where(r => r.OutletId == outletId &&
                              r.Status != "Canceled" &&
                              r.Status != "NoShow" &&
                              r.Status != "Completed" && // Add Completed to the excluded statuses
                              r.ReservationDate < endTime)
                    .ToListAsync();

                // Then apply more precise filtering client-side
                return overlappingReservations
                    .Where(r => r.ReservationDate.Add(r.Duration) > startTime)
                    .Sum(r => r.PartySize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reserved capacity for outlet: {OutletId}", outletId);
                throw;
            }
        }

        public async Task AddTableAssignmentAsync(ReservationTableAssignment tableAssignment)
        {
            _logger.LogInformation("Adding table assignment for reservation: {ReservationId}, table: {TableId}",
                tableAssignment.ReservationId, tableAssignment.TableId);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.TableAssignments.AddAsync(tableAssignment);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding table assignment for reservation: {ReservationId}",
                    tableAssignment.ReservationId);
                throw;
            }
        }

        public async Task RemoveTableAssignmentAsync(Guid reservationId, Guid tableId)
        {
            _logger.LogInformation("Removing table assignment for reservation: {ReservationId}, table: {TableId}",
                reservationId, tableId);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                var assignment = await context.TableAssignments
                    .FirstOrDefaultAsync(ta => ta.ReservationId == reservationId && ta.TableId == tableId);

                if (assignment != null)
                {
                    context.TableAssignments.Remove(assignment);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing table assignment for reservation: {ReservationId}",
                    reservationId);
                throw;
            }
        }

        public async Task AddReminderAsync(ReservationReminder reminder)
        {
            _logger.LogInformation("Adding reminder for reservation: {ReservationId}, type: {ReminderType}",
                reminder.ReservationId, reminder.ReminderType);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.Reminders.AddAsync(reminder);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reminder for reservation: {ReservationId}",
                    reminder.ReservationId);
                throw;
            }
        }

        public async Task UpdateReminderAsync(ReservationReminder reminder)
        {
            _logger.LogInformation("Updating reminder: {ReminderId}", reminder.Id);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                var existingReminder = await context.Reminders.FindAsync(reminder.Id);
                if (existingReminder == null)
                {
                    _logger.LogWarning("Reminder not found for update: {ReminderId}", reminder.Id);
                    throw new KeyNotFoundException($"Reminder with ID {reminder.Id} not found");
                }

                context.Entry(existingReminder).CurrentValues.SetValues(reminder);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reminder: {ReminderId}", reminder.Id);
                throw;
            }
        }

        public async Task AddStatusChangeAsync(ReservationStatusChange statusChange)
        {
            _logger.LogInformation("Adding status change for reservation: {ReservationId}, from {OldStatus} to {NewStatus}",
                statusChange.ReservationId, statusChange.OldStatus, statusChange.NewStatus);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.StatusChanges.AddAsync(statusChange);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding status change for reservation: {ReservationId}",
                    statusChange.ReservationId);
                throw;
            }
        }

        public async Task<IEnumerable<ReservationReminder>> GetPendingRemindersAsync(DateTime before)
        {
            _logger.LogInformation("Getting pending reminders scheduled before: {DateTime}", before);

            using var context = _contextFactory.CreateReadContext();
            return await context.Reminders
                .Include(r => r.Reservation)
                .Where(r => r.Status == "Pending" && r.ScheduledFor <= before)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetReservedTableIdsForTimeSlotAsync(
            Guid outletId, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Getting reserved table IDs for outlet: {OutletId} between {StartTime} and {EndTime}",
                outletId, startTime, endTime);

            using var context = _contextFactory.CreateReadContext();
            // First get the reservations with basic filtering that can be translated to SQL
            var overlappingReservations = await context.Reservations
                .Include(r => r.TableAssignments)
                .Where(r => r.OutletId == outletId &&
                          r.Status != "Canceled" &&
                          r.Status != "NoShow" &&
                          r.Status != "Completed" && // Add Completed to the excluded statuses
                          r.ReservationDate < endTime)
                .ToListAsync();

            // Then apply the more complex filtering in memory
            var filteredReservations = overlappingReservations
                .Where(r => r.ReservationDate.Add(r.Duration) > startTime)
                .ToList();

            // Extract the table IDs
            return filteredReservations
                .SelectMany(r => r.TableAssignments)
                .Select(ta => ta.TableId)
                .ToList();
        }

        public async Task<List<Guid>> GetHeldTableIdsForTimeSlotAsync(
            Guid outletId, DateTime startTime, DateTime endTime, string excludeSessionId = null)
        {
            _logger.LogInformation("Getting held table IDs for outlet: {OutletId} between {StartTime} and {EndTime}",
                outletId, startTime, endTime);

            using var context = _contextFactory.CreateReadContext();
            // Get active holds that overlap with the specified time range
            var activeHolds = await context.TableHolds
                .Where(h => h.OutletId == outletId &&
                           h.IsActive &&
                           h.ReservationDateTime < endTime &&
                           h.ReservationDateTime.AddMinutes(120) > startTime && // Assuming a standard 2-hour hold
                           (excludeSessionId == null || h.SessionId != excludeSessionId))
                .ToListAsync();

            // Extract the table IDs from all active holds
            var heldTableIds = activeHolds
                .SelectMany(h => h.TableIds)
                .Distinct()
                .ToList();

            return heldTableIds;
        }

        public async Task<TableHold> CreateTableHoldAsync(TableHold tableHold)
        {
            _logger.LogInformation("Creating table hold for session: {SessionId}", tableHold.SessionId);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                await context.TableHolds.AddAsync(tableHold);
                await context.SaveChangesAsync();
                return tableHold;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table hold for session: {SessionId}", tableHold.SessionId);
                throw;
            }
        }

        public async Task<TableHold> GetTableHoldBySessionIdAsync(string sessionId)
        {
            _logger.LogInformation("Getting table hold by session ID: {SessionId}", sessionId);

            using var context = _contextFactory.CreateReadContext();
            return await context.TableHolds
                .FirstOrDefaultAsync(h => h.SessionId == sessionId && h.IsActive);
        }

        public async Task<TableHold> GetTableHoldByIdAsync(Guid holdId)
        {
            _logger.LogInformation("Getting table hold by ID: {HoldId}", holdId);

            using var context = _contextFactory.CreateReadContext();
            return await context.TableHolds
                .FirstOrDefaultAsync(h => h.Id == holdId);
        }

        public async Task<bool> ReleaseTableHoldAsync(Guid holdId)
        {
            _logger.LogInformation("Releasing table hold: {HoldId}", holdId);

            using var context = _contextFactory.CreateWriteContext();
            try
            {
                var hold = await context.TableHolds.FindAsync(holdId);
                if (hold == null)
                {
                    _logger.LogWarning("Table hold not found for release: {HoldId}", holdId);
                    return false;
                }

                hold.IsActive = false;
                context.TableHolds.Update(hold);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing table hold: {HoldId}", holdId);
                throw;
            }
        }

        public async Task<List<TableHold>> GetExpiredTableHoldsAsync()
        {
            _logger.LogInformation("Getting expired table holds");

            using var context = _contextFactory.CreateReadContext();
            return await context.TableHolds
                .Where(h => h.IsActive && h.HoldExpiresAt < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<(List<ReservationEntity> Reservations, int TotalCount)> SearchReservationsAsync(
            List<Guid> outletIds,
            string searchTerm,
            List<string> statuses,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize)
        {
            _logger.LogInformation("Executing reservation search query with filters");

            using var context = _contextFactory.CreateReadContext();
            try
            {
                // Start with all reservations
                IQueryable<ReservationEntity> query = context.Reservations
                    .Include(r => r.TableAssignments);

                // Apply outlet filter if specified
                if (outletIds != null && outletIds.Any())
                {
                    query = query.Where(r => outletIds.Contains(r.OutletId));
                }

                // Apply search term if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string searchLower = searchTerm.ToLower();

                    query = query.Where(r =>
                        r.CustomerName.ToLower().Contains(searchLower) ||
                        r.CustomerPhone.ToLower().Contains(searchLower) ||
                        r.CustomerEmail.ToLower().Contains(searchLower) ||
                        r.ReservationCode.ToLower().Contains(searchLower));
                }

                // Apply status filter if specified
                if (statuses != null && statuses.Any())
                {
                    query = query.Where(r => statuses.Contains(r.Status));
                }

                // Apply date range filter if specified
                if (startDate.HasValue)
                {
                    DateTime start = startDate.Value.Date;
                    query = query.Where(r => r.ReservationDate >= start);
                }

                if (endDate.HasValue)
                {
                    DateTime end = endDate.Value.Date.AddDays(1); // Include the entire end date
                    query = query.Where(r => r.ReservationDate < end);
                }

                // Get total count before pagination
                int totalCount = await query.CountAsync();

                // Apply pagination
                int skip = (page - 1) * pageSize;
                var reservations = await query
                    .OrderByDescending(r => r.ReservationDate) // Most recent first
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                return (reservations, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error during reservation search");
                throw;
            }
        }

        public async Task<int> GetReservedTableCapacityAsync(Guid outletId, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Getting total capacity of reserved tables for outlet {OutletId} between {StartTime} and {EndTime}",
                outletId, startTime, endTime);

            using var context = _contextFactory.CreateReadContext();
            try
            {
                // Get all reservations that overlap with the specified time range
                var activeReservations = await context.Reservations
                    .Include(r => r.TableAssignments)
                    .Where(r => r.OutletId == outletId &&
                              r.Status != "Canceled" &&
                              r.Status != "NoShow" &&
                              r.Status != "Completed" &&
                              r.ReservationDate < endTime &&
                              r.ReservationDate.AddMinutes(r.Duration.TotalMinutes) > startTime)
                    .ToListAsync();

                if (!activeReservations.Any())
                {
                    return 0; // No reservations, so no capacity used
                }

                // We need to get the table capacities from the adapter since we don't have Tables in this DbContext
                var allOutletTables = await _outletAdapter.GetTablesAsync(outletId);
                var tableCapacityLookup = allOutletTables.ToDictionary(t => t.Id, t => t.Capacity);

                // Calculate total capacity by adding up the capacity of each assigned table
                int totalReservedCapacity = 0;
                foreach (var reservation in activeReservations)
                {
                    foreach (var tableAssignment in reservation.TableAssignments)
                    {
                        if (tableCapacityLookup.TryGetValue(tableAssignment.TableId, out int capacity))
                        {
                            totalReservedCapacity += capacity;
                        }
                    }
                }

                _logger.LogInformation("Total capacity of reserved tables: {Capacity}", totalReservedCapacity);

                return totalReservedCapacity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total capacity of reserved tables for outlet {OutletId}", outletId);
                // In case of error, fall back to getting reserved capacity by party size
                return await GetReservedCapacityForTimeSlotAsync(outletId, startTime, endTime);
            }
        }

        public async Task<List<ReservationReminder>> GetAllRemindersByReservationIdAsync(Guid reservationId)
        {
            _logger.LogInformation("Getting all reminders for reservation: {ReservationId}", reservationId);

            using var context = _contextFactory.CreateReadContext();
            return await context.Reminders
                .Where(r => r.ReservationId == reservationId)
                .ToListAsync();
        }
    }
}