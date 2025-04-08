using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Reservation.Core.DTOs
{
    /// <summary>
    /// Request DTO for getting availability at nearby outlets
    /// </summary>
    public class NearbyOutletsAvailabilityRequestDto
    {
        [Required(ErrorMessage = "Original outlet ID is required")]
        public Guid OriginalOutletId { get; set; }

        [Required(ErrorMessage = "Party size is required")]
        [Range(1, 50, ErrorMessage = "Party size must be between 1 and 50")]
        public int PartySize { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }

        public TimeSpan? PreferredTime { get; set; }

        /// <summary>
        /// Indicates if the user has granted location permission
        /// </summary>
        public bool HasLocationPermission { get; set; }

        /// <summary>
        /// User's latitude (if location permission granted)
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// User's longitude (if location permission granted)
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Maximum number of nearby outlets to return
        /// </summary>
        [Range(1, 10, ErrorMessage = "Maximum number of nearby outlets must be between 1 and 10")]
        public int MaxNearbyOutlets { get; set; } = 3;
    }

    /// <summary>
    /// Response DTO containing nearby outlets with available time slots
    /// </summary>
    public class NearbyOutletsAvailabilityResponseDto
    {
        public Guid OriginalOutletId { get; set; }
        public string OriginalOutletName { get; set; }
        public List<NearbyOutletAvailabilityDto> NearbyOutlets { get; set; } = new List<NearbyOutletAvailabilityDto>();
    }

    /// <summary>
    /// DTO for a nearby outlet with its available time slots
    /// </summary>
    public class NearbyOutletAvailabilityDto
    {
        public Guid OutletId { get; set; }
        public string OutletName { get; set; }

        /// <summary>
        /// Distance in kilometers (null if location permission not granted)
        /// </summary>
        public double? DistanceKm { get; set; }

        public List<AvailableTimeslotDto> AvailableTimeSlots { get; set; } = new List<AvailableTimeslotDto>();
    }
}