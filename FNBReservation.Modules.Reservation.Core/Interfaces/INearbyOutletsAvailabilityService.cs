using System.Collections.Generic;
using System.Threading.Tasks;
using FNBReservation.Modules.Reservation.Core.DTOs;

namespace FNBReservation.Modules.Reservation.Core.Interfaces
{
    public interface INearbyOutletsAvailabilityService
    {
        /// <summary>
        /// Get availability at nearby outlets when preferred time isn't available at the original outlet
        /// </summary>
        Task<NearbyOutletsAvailabilityResponseDto> GetNearbyOutletsAvailabilityAsync(NearbyOutletsAvailabilityRequestDto request);
    }
}
