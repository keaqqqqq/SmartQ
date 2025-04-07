using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Reservation.Core.DTOs;
using FNBReservation.Modules.Reservation.Core.Entities;

namespace FNBReservation.Modules.Reservation.Core.Interfaces
{
    public interface IReservationService
    {
        Task<TimeSlotAvailabilityResponseDto> CheckAvailabilityAsync(CheckAvailabilityRequestDto request);
        Task<ReservationDto> CreateReservationAsync(CreateReservationDto createReservationDto);
        Task<ReservationDto> GetReservationByIdAsync(Guid id);
        Task<ReservationDto> GetReservationByCodeAsync(string reservationCode);
        Task<IEnumerable<ReservationDto>> GetReservationsByOutletIdAsync(Guid outletId, DateTime? date = null, string status = null);
        Task<IEnumerable<ReservationDto>> GetReservationsByPhoneAsync(string phone);
        Task<ReservationDto> UpdateReservationAsync(Guid id, UpdateReservationDto updateReservationDto);
        Task<ReservationDto> CancelReservationAsync(Guid id, CancelReservationDto cancelReservationDto);
        Task<bool> ConfirmReservationAsync(Guid id);
        Task<bool> MarkAsNoShowAsync(Guid id);
        Task<bool> MarkAsCompletedAsync(Guid id);
        Task SendReservationRemindersAsync();
    }

    public interface IReservationRepository
    {
        Task<ReservationEntity> CreateAsync(ReservationEntity reservation);
        Task<ReservationEntity> GetByIdAsync(Guid id);
        Task<ReservationEntity> GetByCodeAsync(string reservationCode);
        Task<IEnumerable<ReservationEntity>> GetByOutletIdAsync(Guid outletId, DateTime? date = null, string status = null);
        Task<IEnumerable<ReservationEntity>> GetByPhoneAsync(string phone);
        Task<IEnumerable<ReservationEntity>> GetByDateRangeAsync(Guid outletId, DateTime startDate, DateTime endDate);
        Task<ReservationEntity> UpdateAsync(ReservationEntity reservation);
        Task<bool> DeleteAsync(Guid id);
        Task<int> GetReservationCountForTimeSlotAsync(Guid outletId, DateTime startTime, DateTime endTime);
        Task<int> GetReservedCapacityForTimeSlotAsync(Guid outletId, DateTime startTime, DateTime endTime);
        Task AddTableAssignmentAsync(ReservationTableAssignment tableAssignment);
        Task RemoveTableAssignmentAsync(Guid reservationId, Guid tableId);
        Task AddReminderAsync(ReservationReminder reminder);
        Task UpdateReminderAsync(ReservationReminder reminder);
        Task AddStatusChangeAsync(ReservationStatusChange statusChange);
        Task<IEnumerable<ReservationReminder>> GetPendingRemindersAsync(DateTime before);
        Task<List<Guid>> GetReservedTableIdsForTimeSlotAsync(Guid outletId, DateTime startTime, DateTime endTime);

    }

    public interface IReservationNotificationService
    {
        Task SendConfirmationAsync(Guid reservationId);
        Task SendReminderAsync(Guid reservationId, string reminderType);
        Task SendCancellationAsync(Guid reservationId, string reason);
        Task SendModificationAsync(Guid reservationId, string changes);
        Task ProcessPendingRemindersAsync();
    }
}