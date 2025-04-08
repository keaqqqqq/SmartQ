using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.DTOs;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.DTOs;

namespace FNBReservation.Modules.Reservation.Infrastructure.Services
{

    public class NearbyOutletsAvailabilityService : INearbyOutletsAvailabilityService
    {
        private readonly IReservationService _reservationService;
        private readonly IGeolocationService _geolocationService;
        private readonly IOutletService _outletService;
        private readonly ILogger<NearbyOutletsAvailabilityService> _logger;

        public NearbyOutletsAvailabilityService(
            IReservationService reservationService,
            IGeolocationService geolocationService,
            IOutletService outletService,
            ILogger<NearbyOutletsAvailabilityService> logger)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<NearbyOutletsAvailabilityResponseDto> GetNearbyOutletsAvailabilityAsync(NearbyOutletsAvailabilityRequestDto request)
        {
            _logger.LogInformation("Getting nearby outlets availability for outlet {OutletId}, party size {PartySize}, date {Date}",
                request.OriginalOutletId, request.PartySize, request.Date);

            var response = new NearbyOutletsAvailabilityResponseDto
            {
                OriginalOutletId = request.OriginalOutletId,
                NearbyOutlets = new List<NearbyOutletAvailabilityDto>()
            };

            try
            {
                // Get original outlet info
                var originalOutlet = await _outletService.GetOutletByIdAsync(request.OriginalOutletId);
                if (originalOutlet == null)
                {
                    _logger.LogWarning("Original outlet {OutletId} not found", request.OriginalOutletId);
                    throw new ArgumentException($"Outlet with ID {request.OriginalOutletId} not found");
                }

                response.OriginalOutletName = originalOutlet.Name;

                // Find nearby outlets
                List<OutletDto> nearbyOutlets;
                if (request.HasLocationPermission && request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    // Get nearest outlets based on user location
                    nearbyOutlets = await _geolocationService.FindNearestOutletsAsync(
                        request.Latitude.Value,
                        request.Longitude.Value,
                        request.MaxNearbyOutlets + 1); // +1 because the original outlet might be included

                    // Remove the original outlet from the list if it's included
                    nearbyOutlets = nearbyOutlets
                        .Where(o => o.Id != request.OriginalOutletId)
                        .Take(request.MaxNearbyOutlets)
                        .ToList();
                }
                else
                {
                    // Without location permission, just get some other active outlets
                    var allOutlets = await _outletService.GetAllOutletsAsync();
                    nearbyOutlets = allOutlets
                        .Where(o => o.Id != request.OriginalOutletId && o.Status == "Active")
                        .Take(request.MaxNearbyOutlets)
                        .ToList();
                }

                if (!nearbyOutlets.Any())
                {
                    _logger.LogInformation("No nearby outlets found");
                    return response;
                }

                // Check availability for each nearby outlet
                foreach (var outlet in nearbyOutlets)
                {
                    var availabilityRequest = new CheckAvailabilityRequestDto
                    {
                        OutletId = outlet.Id,
                        PartySize = request.PartySize,
                        Date = request.Date,
                        PreferredTime = request.PreferredTime
                    };

                    var availability = await _reservationService.CheckAvailabilityAsync(availabilityRequest);

                    // Calculate distance if location is available
                    double? distanceKm = null;
                    if (request.HasLocationPermission && request.Latitude.HasValue && request.Longitude.HasValue &&
                        outlet.Latitude.HasValue && outlet.Longitude.HasValue)
                    {
                        distanceKm = _geolocationService.CalculateDistance(
                            request.Latitude.Value,
                            request.Longitude.Value,
                            outlet.Latitude.Value,
                            outlet.Longitude.Value);

                        distanceKm = Math.Round(distanceKm.Value, 1);
                    }

                    // Combine preferred and alternative timeslots
                    var allTimeSlots = new List<AvailableTimeslotDto>();
                    allTimeSlots.AddRange(availability.AvailableTimeSlots);
                    allTimeSlots.AddRange(availability.AlternativeTimeSlots);

                    // Only include outlet if it has available time slots
                    if (allTimeSlots.Any())
                    {
                        response.NearbyOutlets.Add(new NearbyOutletAvailabilityDto
                        {
                            OutletId = outlet.Id,
                            OutletName = outlet.Name,
                            DistanceKm = distanceKm,
                            AvailableTimeSlots = allTimeSlots
                                .OrderBy(ts => ts.DateTime)
                                .Take(5) // Limit to 5 time slots per outlet
                                .ToList()
                        });
                    }
                }

                // Sort nearby outlets by distance if available, otherwise by name
                if (request.HasLocationPermission && request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    response.NearbyOutlets = response.NearbyOutlets
                        .OrderBy(o => o.DistanceKm)
                        .ToList();
                }
                else
                {
                    response.NearbyOutlets = response.NearbyOutlets
                        .OrderBy(o => o.OutletName)
                        .ToList();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby outlets availability");
                throw;
            }
        }
    }
}