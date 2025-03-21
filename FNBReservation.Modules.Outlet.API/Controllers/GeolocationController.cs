// FNBReservation.Modules.Outlet.API/Controllers/GeolocationController.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.API.Controllers
{
    [ApiController]
    [Route("api/v1/geolocation")]
    public class GeolocationController : ControllerBase
    {
        private readonly IGeolocationService _geolocationService;
        private readonly ILogger<GeolocationController> _logger;

        public GeolocationController(
            IGeolocationService geolocationService,
            ILogger<GeolocationController> logger)
        {
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("nearest-outlets")]
        public async Task<IActionResult> GetNearestOutlets([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] int limit = 5)
        {
            try
            {
                // Validate coordinates
                if (latitude < -90 || latitude > 90)
                {
                    return BadRequest(new { message = "Latitude must be between -90 and 90" });
                }

                if (longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { message = "Longitude must be between -180 and 180" });
                }

                var nearestOutlets = await _geolocationService.FindNearestOutletsAsync(latitude, longitude, limit);
                return Ok(nearestOutlets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding nearest outlets for coordinates ({Latitude}, {Longitude})", latitude, longitude);
                return StatusCode(500, new { message = "An error occurred while finding the nearest outlets" });
            }
        }
    }
}