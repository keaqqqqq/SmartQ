using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Queue.Core.DTOs
{
    // Request DTOs
    public class CreateQueueEntryDto
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

        [Required(ErrorMessage = "Party size is required")]
        [Range(1, 50, ErrorMessage = "Party size must be between 1 and 50")]
        public int PartySize { get; set; }

        [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
        public string SpecialRequests { get; set; }
    }

    public class UpdateQueueEntryDto
    {
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string CustomerName { get; set; }

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string CustomerPhone { get; set; }

        [Range(1, 50, ErrorMessage = "Party size must be between 1 and 50")]
        public int? PartySize { get; set; }

        [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
        public string SpecialRequests { get; set; }
    }

    public class QueueStatusUpdateDto
    {
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(Called|Seated|Completed|NoShow|Cancelled)$",
            ErrorMessage = "Status must be one of: Called, Seated, Completed, NoShow, Cancelled")]
        public string Status { get; set; }

        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters")]
        public string Reason { get; set; }
    }

    // Response DTOs
    public class QueueEntryDto
    {
        public Guid Id { get; set; }
        public string QueueCode { get; set; }
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int PartySize { get; set; }
        public string SpecialRequests { get; set; }
        public string Status { get; set; }
        public int QueuePosition { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? CalledAt { get; set; }
        public DateTime? SeatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int EstimatedWaitMinutes { get; set; }
        public bool IsHeld { get; set; }
        public DateTime? HeldSince { get; set; }
        public List<TableAssignmentDto> TableAssignments { get; set; } = new List<TableAssignmentDto>();
    }

    public class TableAssignmentDto
    {
        public Guid TableId { get; set; }
        public string TableNumber { get; set; }
        public string Section { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; }
    }

    public class QueueStatusDto
    {
        public Guid QueueEntryId { get; set; }
        public string QueueCode { get; set; }
        public int QueuePosition { get; set; }
        public string Status { get; set; }
        public int EstimatedWaitMinutes { get; set; }
        public int TotalInQueue { get; set; }
    }

    public class QueueSummaryDto
    {
        public int TotalWaiting { get; set; }
        public int TotalCalled { get; set; }
        public int TotalSeated { get; set; }
        public int AverageWaitMinutes { get; set; }
        public int LongestWaitMinutes { get; set; }
    }

    public class QueueEntryListResponseDto
    {
        public List<QueueEntryDto> Entries { get; set; } = new List<QueueEntryDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class TableRecommendationDto
    {
        public Guid QueueEntryId { get; set; }
        public string QueueCode { get; set; }
        public string CustomerName { get; set; }
        public int PartySize { get; set; }
        public Guid TableId { get; set; }
        public string TableNumber { get; set; }
        public int TableCapacity { get; set; }
        public string RecommendationType { get; set; } // "Optimal", "TooSmall", "TooLarge"
        public string RecommendationMessage { get; set; }
    }
}