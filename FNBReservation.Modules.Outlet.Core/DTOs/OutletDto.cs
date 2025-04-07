// FNBReservation.Modules.Outlet.Core/DTOs/OutletDto.cs (Updated for Capacity)
using System;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Outlet.Core.DTOs
{
    public class CreateOutletDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Operating hours are required")]
        [StringLength(100, ErrorMessage = "Operating hours cannot exceed 100 characters")]
        public string OperatingHours { get; set; }

        [Required(ErrorMessage = "Maximum advance reservation time is required")]
        [Range(1, 90, ErrorMessage = "Maximum advance reservation time must be between 1 and 90 days")]
        public int MaxAdvanceReservationTime { get; set; }

        [Required(ErrorMessage = "Minimum advance reservation time is required")]
        [Range(0, 72, ErrorMessage = "Minimum advance reservation time must be between 0 and 72 hours")]
        public int MinAdvanceReservationTime { get; set; }

        [Required(ErrorMessage = "Contact information is required")]
        [StringLength(50, ErrorMessage = "Contact information cannot exceed 50 characters")]
        public string Contact { get; set; }

        public bool QueueEnabled { get; set; } = true;

        public bool SpecialRequirements { get; set; }

        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        public string Status { get; set; } = "Active";

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        // Reservation allocation settings
        [Range(0, 100, ErrorMessage = "Reservation allocation must be between 0 and 100 percent")]
        public int ReservationAllocationPercent { get; set; } = 30; // Default 30% for reservations, 70% for walk-ins

        [Range(30, 240, ErrorMessage = "Default dining duration must be between 30 and 240 minutes")]
        public int DefaultDiningDurationMinutes { get; set; } = 120; // Default 2 hours

    }

    public class UpdateOutletDto
    {
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
        public string? Location { get; set; }

        [StringLength(100, ErrorMessage = "Operating hours cannot exceed 100 characters")]
        public string? OperatingHours { get; set; }

        [Range(1, 90, ErrorMessage = "Maximum advance reservation time must be between 1 and 90 days")]
        public int? MaxAdvanceReservationTime { get; set; }

        [Range(0, 72, ErrorMessage = "Minimum advance reservation time must be between 0 and 72 hours")]
        public int? MinAdvanceReservationTime { get; set; }

        [StringLength(50, ErrorMessage = "Contact information cannot exceed 50 characters")]
        public string? Contact { get; set; }

        public bool? QueueEnabled { get; set; }

        public bool? SpecialRequirements { get; set; }

        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        public string? Status { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        // Reservation allocation settings
        [Range(0, 100, ErrorMessage = "Reservation allocation must be between 0 and 100 percent")]
        public int? ReservationAllocationPercent { get; set; }

        [Range(30, 240, ErrorMessage = "Default dining duration must be between 30 and 240 minutes")]
        public int? DefaultDiningDurationMinutes { get; set; }

    }

    public class OutletDto
    {
        public Guid Id { get; set; }
        public string OutletId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string OperatingHours { get; set; }
        public int Capacity { get; set; } // This is now calculated from tables
        public int MaxAdvanceReservationTime { get; set; }
        public int MinAdvanceReservationTime { get; set; }
        public string Contact { get; set; }
        public bool QueueEnabled { get; set; }
        public bool SpecialRequirements { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int ReservationAllocationPercent { get; set; }
        public int DefaultDiningDurationMinutes { get; set; }
        public List<PeakHourSettingDto> PeakHourSettings { get; set; }

        // Calculated properties based on reservation allocation
        public int ReservationCapacity => (int)Math.Ceiling(Capacity * (ReservationAllocationPercent / 100.0));
        public int WalkInCapacity => Capacity - ReservationCapacity;
    }

    // Existing DTOs remain unchanged
    public class OutletChangeDto
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public string RequestedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewedBy { get; set; }
        public string Comments { get; set; }
    }

    public class OutletChangeResponseDto
    {
        [Required(ErrorMessage = "Change status is required")]
        [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Status must be either 'Approved' or 'Rejected'")]
        public string Status { get; set; }

        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
        public string Comments { get; set; }
    }
}