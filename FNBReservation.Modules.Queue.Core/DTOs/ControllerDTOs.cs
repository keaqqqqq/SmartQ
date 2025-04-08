// FNBReservation.Modules.Queue.Core/DTOs/ControllerDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Queue.Core.DTOs
{
    public class CallNextCustomerDto
    {
        [Required(ErrorMessage = "Table ID is required")]
        public Guid TableId { get; set; }
    }

    public class CancelQueueEntryDto
    {
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters")]
        public string Reason { get; set; }
    }

    // Updated AssignTableDto with QueueEntryId
    public class AssignTableDto
    {
        [Required(ErrorMessage = "Queue entry ID is required")]
        public Guid QueueEntryId { get; set; }

        [Required(ErrorMessage = "Table ID is required")]
        public Guid TableId { get; set; }

        public Guid StaffId { get; set; }

        public bool StaffConfirmedOverflow { get; set; }
        public bool CombineWithExistingTable { get; set; }

    }
}