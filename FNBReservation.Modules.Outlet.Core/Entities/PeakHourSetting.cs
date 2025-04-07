// FNBReservation.Modules.Outlet.Core/Entities/PeakHourSetting.cs
using System;

namespace FNBReservation.Modules.Outlet.Core.Entities
{
    public class PeakHourSetting
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public OutletEntity Outlet { get; set; }
        public string Name { get; set; }
        public string DaysOfWeek { get; set; } // Comma-separated values: "1,2,3,4,5,6,7" (Monday to Sunday)
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int ReservationAllocationPercent { get; set; }
        public bool IsActive { get; set; }
        public bool IsRamadanSetting { get; set; }
        public DateTime? RamadanStartDate { get; set; }
        public DateTime? RamadanEndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}