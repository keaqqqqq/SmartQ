using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class OutletChangeDto
    {
        [JsonPropertyName("changeId")]
        public string ChangeId { get; set; } = string.Empty;

        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("changedBy")]
        public string ChangedBy { get; set; } = string.Empty;

        [JsonPropertyName("changeDate")]
        public DateTime ChangeDate { get; set; } = DateTime.Now;

        [JsonPropertyName("details")]
        public Dictionary<string, object>? Details { get; set; }
    }
}