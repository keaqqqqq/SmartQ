// FNBReservation.Modules.Outlet.Infrastructure/Services/GeolocationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.Infrastructure.Services
{
    public class GeolocationService : IGeolocationService
    {
        private readonly IOutletService _outletService;
        private readonly ILogger<GeolocationService> _logger;
        private const double EarthRadiusKm = 6371.0; // Earth radius in kilometers

        public GeolocationService(
            IOutletService outletService,
            ILogger<GeolocationService> logger)
        {
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<OutletDto>> FindNearestOutletsAsync(double latitude, double longitude, int limit = 5)
        {
            _logger.LogInformation("Finding nearest outlets to coordinates ({Latitude}, {Longitude})", latitude, longitude);

            try
            {
                // Get all active outlets
                var allOutlets = await _outletService.GetAllOutletsAsync();
                var activeOutlets = allOutlets.Where(o => o.Status == "Active").ToList();

                // Filter outlets that have coordinates
                var outletsWithCoordinates = activeOutlets
                    .Where(o => o.Latitude.HasValue && o.Longitude.HasValue)
                    .ToList();

                if (!outletsWithCoordinates.Any())
                {
                    _logger.LogWarning("No outlets found with valid coordinates");
                    return new List<OutletDto>();
                }

                // Calculate distance for each outlet
                var outletsWithDistance = outletsWithCoordinates
                    .Select(outlet => new
                    {
                        Outlet = outlet,
                        Distance = CalculateDistance(latitude, longitude, outlet.Latitude.Value, outlet.Longitude.Value)
                    })
                    .OrderBy(x => x.Distance)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation("Found {Count} nearest outlets", outletsWithDistance.Count);

                return outletsWithDistance.Select(x => x.Outlet).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding nearest outlets");
                return new List<OutletDto>();
            }
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Convert to radians
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            // Haversine formula
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = EarthRadiusKm * c; // Distance in km

            return distance;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}