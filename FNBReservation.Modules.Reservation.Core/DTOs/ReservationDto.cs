using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Reservation.Core.DTOs
{
    // Request DTOs
    public class CreateReservationDto
    {
        [Required(ErrorMessage = "Outlet ID is required")]
        public Guid OutletId { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string CustomerPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "Party size is required")]
        [Range(1, 50, ErrorMessage = "Party size must be between 1 and 50")]
        public int PartySize { get; set; }

        [Required(ErrorMessage = "Reservation date and time is required")]
        public DateTime ReservationDate { get; set; }

        [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
        public string SpecialRequests { get; set; }
    }

    public class UpdateReservationDto
    {
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? CustomerName { get; set; }

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string? CustomerPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? CustomerEmail { get; set; }

        [Range(1, 50, ErrorMessage = "Party size must be between 1 and 50")]
        public int? PartySize { get; set; }

        public DateTime? ReservationDate { get; set; }

        [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
        public string? SpecialRequests { get; set; }
    }

    public class CancelReservationDto
    {
        [Required(ErrorMessage = "Cancellation reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }
    }

    public class CheckAvailabilityRequestDto
    {
        [Required(ErrorMessage = "Outlet ID is required")]
        public Guid OutletId { get; set; }

        [Required(ErrorMessage = "Party size is required")]
        [Range(1, 50, ErrorMessage = "Party size must be between 1 and 50")]
        public int PartySize { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }

        public TimeSpan? PreferredTime { get; set; }

        // Optional parameters to specify a time range for availability search
        public TimeSpan? EarliestTime { get; set; }
        public TimeSpan? LatestTime { get; set; }
    }

    // Response DTOs
    public class ReservationDto
    {
        public Guid Id { get; set; }
        public string ReservationCode { get; set; }
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public int PartySize { get; set; }
        public DateTime ReservationDate { get; set; }
        public TimeSpan Duration { get; set; }
        public string Status { get; set; }
        public string SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<TableAssignmentDto> TableAssignments { get; set; } = new List<TableAssignmentDto>();
    }

    public class TableAssignmentDto
    {
        public Guid TableId { get; set; }
        public string TableNumber { get; set; }
        public string Section { get; set; }
        public int Capacity { get; set; }
    }

    public class AvailableTimeslotDto
    {
        public DateTime DateTime { get; set; }
        public int AvailableCapacity { get; set; }
        public bool IsPreferred { get; set; }
    }

    public class TimeSlotAvailabilityResponseDto
    {
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public int PartySize { get; set; }
        public DateTime Date { get; set; }
        public List<AvailableTimeslotDto> AvailableTimeSlots { get; set; } = new List<AvailableTimeslotDto>();
        public List<AvailableTimeslotDto> AlternativeTimeSlots { get; set; } = new List<AvailableTimeslotDto>();
    }
}