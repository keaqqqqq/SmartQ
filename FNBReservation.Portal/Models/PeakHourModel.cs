using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class PeakHour
    {
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
    }

    public class TableInfo
    {
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; } = 2;

        [JsonPropertyName("section")]
        public string Section { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }
}