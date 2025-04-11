using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.Entities;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Reservation.Infrastructure.Data;

namespace FNBReservation.Modules.Reservation.Infrastructure.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ReservationDbContext _dbContext;
        private readonly ILogger<ReservationRepository> _logger;
        private readonly IOutletAdapter _outletAdapter; // New field


        public ReservationRepository(ReservationDbContext dbContext, ILogger<ReservationRepository> logger, IOutletAdapter outletAdapter)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _outletAdapter = outletAdapter ?? throw new ArgumentNullException(nameof(outletAdapter));

        }

        public async Task<ReservationEntity> CreateAsync(ReservationEntity reservation)
        {
            _logger.LogInformation("Creating new reservation for {CustomerName} on {ReservationDate}",
                reservation.CustomerName, reservation.ReservationDate);

            await _dbContext.Reservations.AddAsync(reservation);
            await _dbContext.SaveChangesAsync();

            return reservation;
        }

        public async Task<ReservationEntity> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting reservation by ID: {ReservationId}", id);

            return await _dbContext.Reservations
                .Include(r => r.TableAssignments)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<ReservationEntity> GetByCodeAsync(string reservationCode)
        {
            _logger.LogInformation("Getting reservation by code: {ReservationCode}", reservationCode);

            return await _dbContext.Reservations
                .Include(r => r.TableAssignments)
                .FirstOrDefaultAsync(r => r.ReservationCode == reservationCode);
        }

        public async Task<IEnumerable<ReservationEntity>> GetByOutletIdAsync(Guid outletId, DateTime? date = null, string status = null)
        {
            _logger.LogInformation("Getting reservations for outlet: {OutletId}, Date: {Date}, Status: {Status}",
                outletId, date, status);

            var query = _dbContext.Reservations
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

            return await _dbContext.Reservations
                .Include(r => r.TableAssignments)
                .Where(r => r.CustomerPhone == phone)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReservationEntity>> GetByDateRangeAsync(Guid outletId, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Getting reservations for outlet: {OutletId} between {StartDate} and {EndDate}",
                outletId, startDate, endDate);

            return await _dbContext.Reservations
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

            _dbContext.Reservations.Update(reservation);
            await _dbContext.SaveChangesAsync();

            return reservation;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting reservation: {ReservationId}", id);

            var reservation = await _dbContext.Reservations.FindAsync(id);
            if (reservation == null)
            {
                _logger.LogWarning("Reservation not found for deletion: {ReservationId}", id);
                return false;
            }

            _dbContext.Reservations.Remove(reservation);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetReservationCountForTimeSlotAsync(Guid outletId, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Getting reservation count for outlet: {OutletId} between {StartTime} and {EndTime}",
                outletId, startTime, endTime);

            // First, get the reservations that might overlap
            var overlappingReservations = await _dbContext.Reservations
                .Where(r => r.OutletId == outletId &&
                           r.Status != "Canceled" &&
                           r.Status != "NoShow" &&
                           r.ReservationDate < endTime)
                .ToListAsync();

            // Then filter and count client-side
            return overlappingReservations
                .Count(r => r.ReservationDate.Add(r.Duration) > startTime);
        }

        public async Task<int> GetReservedCapacityForTimeSlotAsync(Guid outletId, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Getting reserved capacity for outlet: {OutletId} between {StartTime} and {EndTime}",
                outletId, startTime, endTime);

            // Use a transaction for consistent reads
            using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            // Get all active reservations for this time slot with a more accurate overlap check
            var overlappingReservations = await _dbContext.Reservations
                .Where(r => r.OutletId == outletId &&
                          r.Status != "Canceled" &&
                          r.Status != "NoShow" &&
                          r.Status != "Completed" && // Add Completed to the excluded statuses
                          r.ReservationDate < endTime)
                .ToListAsync();

            // Complete the transaction
            await transaction.CommitAsync();

            // Then apply more precise filtering client-side
            return overlappingReservations
                .Where(r => r.ReservationDate.Add(r.Duration) > startTime)
                .Sum(r => r.PartySize);
        }

        public async Task AddTableAssignmentAsync(ReservationTableAssignment tableAssignment)
        {
            _logger.LogInformation("Adding table assignment for reservation: {ReservationId}, table: {TableId}",
                tableAssignment.ReservationId, tableAssignment.TableId);

            await _dbContext.TableAssignments.AddAsync(tableAssignment);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveTableAssignmentAsync(Guid reservationId, Guid tableId)
        {
            _logger.LogInformation("Removing table assignment for reservation: {ReservationId}, table: {TableId}",
                reservationId, tableId);

            var assignment = await _dbContext.TableAssignments
                .FirstOrDefaultAsync(ta => ta.ReservationId == reservationId && ta.TableId == tableId);

            if (assignment != null)
            {
                _dbContext.TableAssignments.Remove(assignment);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task AddReminderAsync(ReservationReminder reminder)
        {
            _logger.LogInformation("Adding reminder for reservation: {ReservationId}, type: {ReminderType}",
                reminder.ReservationId, reminder.ReminderType);

            await _dbContext.Reminders.AddAsync(reminder);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateReminderAsync(ReservationReminder reminder)
        {
            _logger.LogInformation("Updating reminder: {ReminderId}", reminder.Id);

            _dbContext.Reminders.Update(reminder);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddStatusChangeAsync(ReservationStatusChange statusChange)
        {
            _logger.LogInformation("Adding status change for reservation: {ReservationId}, from {OldStatus} to {NewStatus}",
                statusChange.ReservationId, statusChange.OldStatus, statusChange.NewStatus);

            await _dbContext.StatusChanges.AddAsync(statusChange);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<ReservationReminder>> GetPendingRemindersAsync(DateTime before)
        {
            _logger.LogInformation("Getting pending reminders scheduled before: {DateTime}", before);

            return await _dbContext.Reminders
                .Include(r => r.Reservation)
                .Where(r => r.Status == "Pending" && r.ScheduledFor <= before)
                .ToListAsync();
        }

        // Add this method to ReservationRepository
        public async Task<List<Guid>> GetReservedTableIdsForTimeSlotAsync(
            Guid outletId, DateTime startTime, DateTime endTime)
        {
            // First get the reservations with basic filtering that can be translated to SQL
            var overlappingReservations = await _dbContext.Reservations
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

            // Get active holds that overlap with the specified time range
            var activeHolds = await _dbContext.TableHolds
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
            await _dbContext.TableHolds.AddAsync(tableHold);
            await _dbContext.SaveChangesAsync();
            return tableHold;
        }

        public async Task<TableHold> GetTableHoldBySessionIdAsync(string sessionId)
        {
            return await _dbContext.TableHolds
                .FirstOrDefaultAsync(h => h.SessionId == sessionId && h.IsActive);
        }

        public async Task<TableHold> GetTableHoldByIdAsync(Guid holdId)
        {
            return await _dbContext.TableHolds
                .FirstOrDefaultAsync(h => h.Id == holdId);
        }

        public async Task<bool> ReleaseTableHoldAsync(Guid holdId)
        {
            var hold = await _dbContext.TableHolds.FindAsync(holdId);
            if (hold == null)
                return false;

            hold.IsActive = false;
            _dbContext.TableHolds.Update(hold);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<TableHold>> GetExpiredTableHoldsAsync()
        {
            return await _dbContext.TableHolds
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

            try
            {
                // Start with all reservations
                IQueryable<ReservationEntity> query = _dbContext.Reservations
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

            try
            {
                // Get all reservations that overlap with the specified time range
                var activeReservations = await _dbContext.Reservations
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
            return await _dbContext.Reminders
                .Where(r => r.ReservationId == reservationId)
                .ToListAsync();
        }
    }
}