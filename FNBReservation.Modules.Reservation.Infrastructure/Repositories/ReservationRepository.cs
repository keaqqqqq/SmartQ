using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public ReservationRepository(ReservationDbContext dbContext, ILogger<ReservationRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    }
}