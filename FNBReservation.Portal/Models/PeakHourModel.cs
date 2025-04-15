using System.Text.Json.Serialization;
using System;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Portal.Models
{
    public class PeakHour
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("daysOfWeek")]
        public string DaysOfWeek { get; set; } = "1,2,3,4,5,6,7";

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = "18:00:00";

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = "20:00:00";

        [JsonPropertyName("reservationAllocationPercent")]
        public int ReservationAllocationPercent { get; set; } = 100;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }
        
        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;
    }

    public class TableInfo
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.Empty;

        [JsonPropertyName("outletId")]
        public Guid OutletId { get; set; } = Guid.Empty;

        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; } = 2;

        [JsonPropertyName("section")]
        public string Section { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

    public class CreateTableRequest
    {
        [Required(ErrorMessage = "Table number is required")]
        [StringLength(20, ErrorMessage = "Table number cannot exceed 20 characters")]
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Section is required")]
        [StringLength(50, ErrorMessage = "Section cannot exceed 50 characters")]
        [JsonPropertyName("section")]
        public string Section { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }
    
    public class UpdateTableRequest
    {
        [StringLength(20, ErrorMessage = "Table number cannot exceed 20 characters")]
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }

        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
        [JsonPropertyName("capacity")]
        public int? Capacity { get; set; }

        [StringLength(50, ErrorMessage = "Section cannot exceed 50 characters")]
        [JsonPropertyName("section")]
        public string Section { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }
    }

    public class SectionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("tableCount")]
        public int TableCount { get; set; }
        
        [JsonPropertyName("totalCapacity")]
        public int TotalCapacity { get; set; }
    }
}