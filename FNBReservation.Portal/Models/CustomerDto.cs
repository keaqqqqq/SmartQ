using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class CustomerDto
    {
        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("isBanned")]
        public bool IsBanned { get; set; } = false;

        [JsonPropertyName("banReason")]
        public string? BanReason { get; set; }

        [JsonPropertyName("bannedDate")]
        public DateTime? BannedDate { get; set; }

        [JsonPropertyName("bannedBy")]
        public string? BannedBy { get; set; }

        [JsonPropertyName("banExpiryDate")]
        public DateTime? BanExpiryDate { get; set; }

        [JsonPropertyName("totalReservations")]
        public int TotalReservations { get; set; } = 0;

        [JsonPropertyName("noShows")]
        public int NoShows { get; set; } = 0;

        [JsonPropertyName("lastVisit")]
        public DateTime? LastVisit { get; set; }

        [JsonPropertyName("firstVisit")]
        public DateTime? FirstVisit { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("reservationHistory")]
        public List<ReservationHistoryItem> ReservationHistory { get; set; } = new List<ReservationHistoryItem>();
    }

    public class ReservationHistoryItem
    {
        [JsonPropertyName("reservationId")]
        public string ReservationId { get; set; } = string.Empty;

        [JsonPropertyName("reservationCode")]
        public string ReservationCode { get; set; } = string.Empty;

        [JsonPropertyName("reservationDate")]
        public DateTime ReservationDate { get; set; }

        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("outletName")]
        public string OutletName { get; set; } = string.Empty;

        [JsonPropertyName("guestCount")]
        public int GuestCount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}