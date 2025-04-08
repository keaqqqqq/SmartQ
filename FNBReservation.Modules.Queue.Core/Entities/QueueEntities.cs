using System;
using System.Collections.Generic;

namespace FNBReservation.Modules.Queue.Core.Entities
{
    public class QueueEntry
    {
        public Guid Id { get; set; }
        public string QueueCode { get; set; } // Unique identifier like Q001
        public Guid OutletId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int PartySize { get; set; }
        public string SpecialRequests { get; set; }
        public string Status { get; set; } // Waiting, Called, Seated, Completed, NoShow, Cancelled
        public int QueuePosition { get; set; } // Current position in queue
        public DateTime QueuedAt { get; set; }
        public DateTime? CalledAt { get; set; }
        public DateTime? SeatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsHeld { get; set; } // If customer is being held for table optimization
        public DateTime? HeldSince { get; set; } // When the customer was put on hold
        public int EstimatedWaitMinutes { get; set; } // Estimated wait time in minutes
        public ICollection<QueueStatusChange> StatusChanges { get; set; } = new List<QueueStatusChange>();
        public ICollection<QueueTableAssignment> TableAssignments { get; set; } = new List<QueueTableAssignment>();
        public ICollection<QueueNotification> Notifications { get; set; } = new List<QueueNotification>();
    }

    public class QueueStatusChange
    {
        public Guid Id { get; set; }
        public Guid QueueEntryId { get; set; }
        public QueueEntry QueueEntry { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public Guid? ChangedById { get; set; } // Staff ID who made the change, null if system
        public string Reason { get; set; }
    }

    public class QueueTableAssignment
    {
        public Guid Id { get; set; }
        public Guid QueueEntryId { get; set; }
        public QueueEntry QueueEntry { get; set; }
        public Guid TableId { get; set; }
        public string TableNumber { get; set; } // Denormalized for convenience
        public string Status { get; set; } // Assigned, Seated, Completed, Cancelled
        public DateTime AssignedAt { get; set; }
        public DateTime? SeatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid AssignedBy { get; set; } // Staff ID who assigned the table
    }

    public class QueueNotification
    {
        public Guid Id { get; set; }
        public Guid QueueEntryId { get; set; }
        public QueueEntry QueueEntry { get; set; }
        public string NotificationType { get; set; } // Confirmation, TableReady, Reminder, etc.
        public string Channel { get; set; } // WhatsApp, SMS, etc.
        public string Content { get; set; }
        public string Status { get; set; } // Pending, Sent, Failed
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}