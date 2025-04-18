﻿using System.Text.Json.Serialization;

namespace FNBReservation.Portal.Models
{
    public class ReservationDto
    {
        [JsonPropertyName("id")]
        public string ReservationId { get; set; } = string.Empty;

        [JsonPropertyName("reservationCode")]
        public string ReservationCode { get; set; } = string.Empty;

        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("outletName")]
        public string OutletName { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("customerPhone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [JsonPropertyName("customerEmail")]
        public string? CustomerEmail { get; set; }

        [JsonPropertyName("partySize")]
        public int PartySize { get; set; }

        [JsonPropertyName("reservationDate")]
        public DateTime ReservationDate { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; } = "01:30:00";

        [JsonIgnore]
        public DateTime EndTime => ReservationDate.Add(TimeSpan.Parse(Duration));

        [JsonPropertyName("tableAssignments")]
        public List<TableAssignment> TableAssignments { get; set; } = new List<TableAssignment>();

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Confirmed";

        [JsonPropertyName("source")]
        public string Source { get; set; } = "Website";

        [JsonPropertyName("specialRequests")]
        public string? SpecialRequests { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("checkInTime")]
        public DateTime? CheckInTime { get; set; }

        [JsonPropertyName("checkOutTime")]
        public DateTime? CheckOutTime { get; set; }

        // Helper property to get table numbers as a list of strings (for display)
        [JsonIgnore]
        public List<string> TableNumbers => TableAssignments?.Select(t => t.TableNumber).ToList() ?? new List<string>();
    }

    public class TableAssignment
    {
        [JsonPropertyName("tableId")]
        public string TableId { get; set; } = string.Empty;

        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; } = string.Empty;

        [JsonPropertyName("section")]
        public string Section { get; set; } = string.Empty;

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
    }

    public class CreateReservationDto
    {
        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("customerPhone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [JsonPropertyName("customerEmail")]
        public string? CustomerEmail { get; set; }

        [JsonPropertyName("partySize")]
        public int PartySize { get; set; }

        [JsonPropertyName("reservationDate")]
        public DateTime ReservationDate { get; set; }

        [JsonPropertyName("specialRequests")]
        public string? SpecialRequests { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = "Website";
    }

    public class UpdateReservationDto
    {
        [JsonPropertyName("reservationId")]
        public string ReservationId { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customerPhone")]
        public string? CustomerPhone { get; set; }

        [JsonPropertyName("customerEmail")]
        public string? CustomerEmail { get; set; }

        [JsonPropertyName("partySize")]
        public int? PartySize { get; set; }

        [JsonPropertyName("reservationDate")]
        public DateTime? ReservationDate { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("tableNumbers")]
        public List<string>? TableAssignments { get; set; }

        [JsonPropertyName("specialRequests")]
        public string? SpecialRequests { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class AvailabilityRequestDto
    {
        [JsonPropertyName("outletId")]
        public string OutletId { get; set; } = string.Empty;

        [JsonPropertyName("partySize")]
        public int PartySize { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("preferredTime")]
        public string PreferredTime { get; set; } = string.Empty;

        [JsonPropertyName("earliestTime")]
        public string EarliestTime { get; set; } = string.Empty;

        [JsonPropertyName("latestTime")]
        public string LatestTime { get; set; } = string.Empty;
    }

    public class AvailabilityResponseDto
    {
        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("availableTimes")]
        public List<AvailableTimeSlot> AvailableTimes { get; set; } = new List<AvailableTimeSlot>();

        [JsonPropertyName("nextAvailableDate")]
        public DateTime? NextAvailableDate { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class AvailableTimeSlot
    {
        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("availableTables")]
        public int AvailableTables { get; set; }
    }

    public class ReservationFilterDto
    {
        public string? OutletId { get; set; }
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
    }
}