// FNBReservation.Modules.Outlet.Core/Entities/Outlet.cs
using System;
using System.Collections.Generic;

namespace FNBReservation.Modules.Outlet.Core.Entities
{
    public class OutletEntity
    {
        public Guid Id { get; set; }
        public string OutletId { get; set; } // Business identifier
        public string Name { get; set; }
        public string Location { get; set; }
        public string OperatingHours { get; set; }
        public int MaxAdvanceReservationTime { get; set; } // In days
        public int MinAdvanceReservationTime { get; set; } // In hours
        public string Contact { get; set; }
        public bool QueueEnabled { get; set; }
        public bool SpecialRequirements { get; set; }
        public string Status { get; set; } // Active, Inactive, Maintenance
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // New reservation settings
        public int ReservationAllocationPercent { get; set; } = 30; // Default 30% for reservations, 70% for walk-ins
        public int DefaultDiningDurationMinutes { get; set; } = 120; // Default 2 hours

        public ICollection<OutletChange> OutletChanges { get; set; } = new List<OutletChange>();
        public ICollection<PeakHourSetting> PeakHourSettings { get; set; } = new List<PeakHourSetting>();
        public ICollection<TableEntity> Tables { get; set; } = new List<TableEntity>();
    }

    public class OutletChange
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public OutletEntity Outlet { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected
        public DateTime RequestedAt { get; set; }
        public Guid RequestedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedBy { get; set; }
        public string Comments { get; set; }
    }
}