using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Customer.Core.DTOs;
using FNBReservation.Modules.Customer.Core.Interfaces;
using FNBReservation.Modules.Reservation.Core.Interfaces;

namespace FNBReservation.Modules.Customer.Infrastructure.Adapters
{
    public class ReservationAdapter : IReservationAdapter
    {
        private readonly IReservationService _reservationService;
        private readonly ILogger<ReservationAdapter> _logger;

        public ReservationAdapter(
            IReservationService reservationService,
            ILogger<ReservationAdapter> logger)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CustomerReservationDto>> GetReservationsByCustomerPhoneAsync(string phone, Guid? outletId = null)
        {
            _logger.LogInformation("Getting reservations for customer phone: {Phone}, outlet: {OutletId}", phone, outletId);

            try
            {
                // Get reservations from the Reservation module
                var reservations = await _reservationService.GetReservationsByPhoneAsync(phone);

                // Filter by outlet if specified
                if (outletId.HasValue)
                {
                    reservations = reservations.Where(r => r.OutletId == outletId.Value).ToList();
                }

                // Map to our DTO format
                return reservations.Select(r => new CustomerReservationDto
                {
                    ReservationId = r.Id,
                    ReservationCode = r.ReservationCode,
                    Date = r.ReservationDate,
                    OutletId = r.OutletId,
                    OutletName = r.OutletName,
                    PartySize = r.PartySize,
                    Status = r.Status,
                    SpecialRequests = r.SpecialRequests
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for phone: {Phone}", phone);
                return new List<CustomerReservationDto>();
            }
        }

        public async Task<(int TotalReservations, int NoShows, DateTime? FirstVisit, DateTime? LastVisit)> GetCustomerStatsAsync(string phone, Guid? outletId = null)
        {
            _logger.LogInformation("Getting stats for customer phone: {Phone}, outlet: {OutletId}", phone, outletId);

            try
            {
                // Get reservations from the Reservation module
                var reservations = await _reservationService.GetReservationsByPhoneAsync(phone);

                // Filter by outlet if specified
                if (outletId.HasValue)
                {
                    reservations = reservations.Where(r => r.OutletId == outletId.Value).ToList();
                }

                // Calculate stats
                int totalReservations = reservations.Count();
                int noShows = reservations.Count(r => r.Status == "NoShow");

                var completedReservations = reservations
                    .Where(r => r.Status == "Completed")
                    .OrderBy(r => r.ReservationDate)
                    .ToList();

                DateTime? firstVisit = completedReservations.Any() ? completedReservations.First().ReservationDate : null;
                DateTime? lastVisit = completedReservations.Any() ? completedReservations.Last().ReservationDate : null;

                return (totalReservations, noShows, firstVisit, lastVisit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for phone: {Phone}", phone);
                return (0, 0, null, null);
            }
        }
    }
}