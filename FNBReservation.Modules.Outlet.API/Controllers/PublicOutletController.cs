// FNBReservation.Modules.Outlet.API/Controllers/PublicOutletController.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Outlet.API.Controllers
{
    [ApiController]
    [Route("api/v1/public/outlets")]
    public class PublicOutletController : ControllerBase
    {
        private readonly IOutletService _outletService;
        private readonly IGeolocationService _geolocationService;
        private readonly ILogger<PublicOutletController> _logger;

        public PublicOutletController(
            IOutletService outletService,
            IGeolocationService geolocationService,
            ILogger<PublicOutletController> logger)
        {
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _geolocationService = geolocationService ?? throw new ArgumentNullException(nameof(geolocationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOutlets()
        {
            try
            {
                _logger.LogInformation("Public API: Getting all active outlets");

                var allOutlets = await _outletService.GetAllOutletsAsync();

                // Filter to only show active outlets to the public
                var activeOutlets = new List<OutletDto>();
                foreach (var outlet in allOutlets)
                {
                    if (outlet.Status == "Active")
                    {
                        // Create a simplified version with only necessary information
                        var publicOutlet = new
                        {
                            Id = outlet.Id,
                            Name = outlet.Name,
                            Location = outlet.Location,
                            OperatingHours = outlet.OperatingHours,
                            Contact = outlet.Contact,
                            Capacity = outlet.Capacity,
                            QueueEnabled = outlet.QueueEnabled,
                            SpecialRequirements = outlet.SpecialRequirements,
                            Latitude = outlet.Latitude,
                            Longitude = outlet.Longitude
                        };

                        activeOutlets.Add(outlet);
                    }
                }

                return Ok(activeOutlets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public outlets list");
                return StatusCode(500, new { message = "An error occurred while retrieving outlets" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOutlet(Guid id)
        {
            try
            {
                _logger.LogInformation("Public API: Getting outlet details: {Id}", id);

                var outlet = await _outletService.GetOutletByIdAsync(id);

                if (outlet == null)
                    return NotFound(new { message = "Outlet not found" });

                // Only allow viewing active outlets
                if (outlet.Status != "Active")
                    return NotFound(new { message = "Outlet not found" });

                // Create a simplified version with only necessary information
                var publicOutlet = new
                {
                    Id = outlet.Id,
                    Name = outlet.Name,
                    Location = outlet.Location,
                    OperatingHours = outlet.OperatingHours,
                    Contact = outlet.Contact,
                    Capacity = outlet.Capacity,
                    QueueEnabled = outlet.QueueEnabled,
                    SpecialRequirements = outlet.SpecialRequirements,
                    Latitude = outlet.Latitude,
                    Longitude = outlet.Longitude
                };

                return Ok(publicOutlet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public outlet details: {OutletId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the outlet" });
            }
        }
    }
}