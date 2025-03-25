using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class StaffDto
    {
        [JsonPropertyName("staffId")]
        public string StaffId { get; set; } = string.Empty;

        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class OutletSummaryDto
    {
        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;
    }
}