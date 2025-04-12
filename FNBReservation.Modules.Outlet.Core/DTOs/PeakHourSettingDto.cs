// FNBReservation.Modules.Outlet.Core/DTOs/PeakHourSettingDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Outlet.Core.DTOs
{
    public class PeakHourSettingDto
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public string Name { get; set; }
        public string DaysOfWeek { get; set; } // Comma-separated values: "1,2,3,4,5,6,7" (Monday to Sunday)
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int ReservationAllocationPercent { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreatePeakHourSettingDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Days of week are required")]
        [RegularExpression("^[1-7](,[1-7])*$", ErrorMessage = "Days of week must be comma-separated values from 1-7")]
        public string DaysOfWeek { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Reservation allocation percentage is required")]
        [Range(0, 100, ErrorMessage = "Reservation allocation must be between 0 and 100 percent")]
        public int ReservationAllocationPercent { get; set; }

        public bool IsActive { get; set; } = true;

    }

    public class UpdatePeakHourSettingDto
    {
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string? Name { get; set; }

        [RegularExpression("^[1-7](,[1-7])*$", ErrorMessage = "Days of week must be comma-separated values from 1-7")]
        public string? DaysOfWeek { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Range(0, 100, ErrorMessage = "Reservation allocation must be between 0 and 100 percent")]
        public int? ReservationAllocationPercent { get; set; }

        public bool? IsActive { get; set; }
    }
}