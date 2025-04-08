namespace FNBReservation.Modules.Reservation.Core.Entities
{
    public class ReservationEntity
    {
        public Guid Id { get; set; }
        public string ReservationCode { get; set; } // Unique business identifier
        public Guid OutletId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public int PartySize { get; set; }
        public DateTime ReservationDate { get; set; } // Date and time combined
        public TimeSpan Duration { get; set; } // From outlet settings
        public string Status { get; set; } // Pending, Confirmed, Canceled, Completed, NoShow
        public string SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<ReservationTableAssignment> TableAssignments { get; set; } = new List<ReservationTableAssignment>();
        public ICollection<ReservationReminder> Reminders { get; set; } = new List<ReservationReminder>();
        public ICollection<ReservationStatusChange> StatusChanges { get; set; } = new List<ReservationStatusChange>();
    }

    public class ReservationTableAssignment
    {
        public Guid Id { get; set; }
        public Guid ReservationId { get; set; }
        public ReservationEntity Reservation { get; set; }
        public Guid TableId { get; set; }
        public string TableNumber { get; set; } // Denormalized for convenience
        public DateTime CreatedAt { get; set; }
    }

    public class ReservationReminder
    {
        public Guid Id { get; set; }
        public Guid ReservationId { get; set; }
        public ReservationEntity Reservation { get; set; }
        public string ReminderType { get; set; } // Confirmation, 24Hour, 1Hour, etc.
        public DateTime ScheduledFor { get; set; }
        public DateTime? SentAt { get; set; }
        public string Status { get; set; } // Pending, Sent, Failed
        public string Channel { get; set; } // WhatsApp, SMS, Email
        public string Content { get; set; } // The actual message sent
    }

    public class ReservationStatusChange
    {
        public Guid Id { get; set; }
        public Guid ReservationId { get; set; }
        public ReservationEntity Reservation { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public Guid? ChangedBy { get; set; } // User ID if changed by staff, null if by system
        public string Reason { get; set; }
    }
    public class TableHold
    {
        public Guid Id { get; set; }
        public Guid OutletId { get; set; }
        public List<Guid> TableIds { get; set; } = new List<Guid>();
        public int PartySize { get; set; }
        public DateTime ReservationDateTime { get; set; }
        public DateTime HoldCreatedAt { get; set; }
        public DateTime HoldExpiresAt { get; set; }
        public string SessionId { get; set; } // To track which user session has the hold
        public bool IsActive { get; set; } = true;
    }
}