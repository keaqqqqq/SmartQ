using System;
using System.ComponentModel.DataAnnotations;

namespace FNBReservation.Modules.Reservation.Core.DTOs
{
    /// <summary>
    /// Combined request DTO for checking availability at an outlet 
    /// and optionally at nearby outlets if preferred time is not available
    /// </summary>
    public class CheckAvailabilityWithNearbyRequestDto
    {
        [Required(ErrorMessage = "Outlet ID is required")]
        public Guid OutletId { get; set; }

        [Required(ErrorMessage = "Party size is required")]
        [Range(1, 50, ErrorMessage = "Party size must be between 1 and 50")]
        public int PartySize { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }

        public TimeSpan? PreferredTime { get; set; }

        /// <summary>
        /// Whether to check availability at nearby outlets if preferred time is not available
        /// </summary>
        public bool CheckNearbyOutlets { get; set; } = false;

        /// <summary>
        /// Whether the user has granted location permission
        /// </summary>
        public bool HasLocationPermission { get; set; } = false;

        /// <summary>
        /// User's latitude (if location permission granted)
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// User's longitude (if location permission granted)
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Maximum number of nearby outlets to check
        /// </summary>
        [Range(1, 10, ErrorMessage = "Maximum number of nearby outlets must be between 1 and 10")]
        public int MaxNearbyOutlets { get; set; } = 3;
    }

    /// <summary>
    /// Combined response DTO with availability at the original outlet 
    /// and optionally at nearby outlets
    /// </summary>
    public class CheckAvailabilityWithNearbyResponseDto
    {
        /// <summary>
        /// Availability at the original outlet
        /// </summary>
        public TimeSlotAvailabilityResponseDto OriginalOutletAvailability { get; set; }

        /// <summary>
        /// Availability at nearby outlets (null if preferred time is available at original outlet)
        /// </summary>
        public NearbyOutletsAvailabilityResponseDto NearbyOutletsAvailability { get; set; }
    }
}