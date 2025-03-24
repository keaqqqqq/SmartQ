using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class OutletDto
    {
        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("operatingHours")]
        public string OperatingHours { get; set; } = string.Empty;

        [JsonPropertyName("maxAdvanceReservationTime")]
        public int MaxAdvanceReservationTime { get; set; } = 30; // Default 30 days

        [JsonPropertyName("minAdvanceReservationTime")]
        public int MinAdvanceReservationTime { get; set; } = 2; // Default 2 hours

        [JsonPropertyName("contact")]
        public string Contact { get; set; } = string.Empty;

        [JsonPropertyName("queueEnabled")]
        public bool QueueEnabled { get; set; } = true;

        [JsonPropertyName("specialRequirements")]
        public bool SpecialRequirements { get; set; } = false;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; } = 0.0;

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; } = 0.0;

        [JsonPropertyName("reservationAllocationPercent")]
        public int ReservationAllocationPercent { get; set; } = 40;

        [JsonPropertyName("defaultDiningDurationMinutes")]
        public int DefaultDiningDurationMinutes { get; set; } = 90;

        [JsonPropertyName("tables")]
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();

        [JsonPropertyName("peakHours")]
        public List<PeakHour> PeakHours { get; set; } = new List<PeakHour>();
    }
}