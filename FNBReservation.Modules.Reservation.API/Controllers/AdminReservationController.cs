// In FNBReservation.Modules.Reservation.API/Controllers/AdminReservationController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.DTOs;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using FNBReservation.Modules.Outlet.Core.Interfaces;

namespace FNBReservation.Modules.Reservation.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/reservations")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly IOutletService _outletService; // Add this
        private readonly ILogger<AdminReservationController> _logger;

        public AdminReservationController(
            IReservationService reservationService,
            IOutletService outletService, // Add this parameter
            ILogger<AdminReservationController> logger)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService)); // Add this
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchReservations(
            [FromQuery] List<Guid> outletIds = null,
            [FromQuery] string searchTerm = "",
            [FromQuery] List<string> statuses = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _reservationService.SearchReservationsAsync(
                    outletIds,
                    searchTerm,
                    statuses,
                    startDate,
                    endDate,
                    page,
                    pageSize,
                    isAdmin: true);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching reservations");
                return StatusCode(500, new { message = "An error occurred while searching reservations" });
            }
        }

        // In AdminReservationController.cs
        [HttpGet("outlets")]
        public async Task<IActionResult> GetOutletsForAdmin()
        {
            try
            {
                var outlets = await _outletService.GetAllOutletsAsync();
                var outletOptions = outlets.Select(o => new
                {
                    Id = o.Id,
                    Name = o.Name,
                    Status = o.Status
                }).ToList();

                return Ok(outletOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving outlets for admin");
                return StatusCode(500, new { message = "An error occurred while retrieving outlets" });
            }
        }

        // In AdminReservationController.cs (and also in OutletReservationController for staff)
        [HttpGet("statuses")]
        public IActionResult GetReservationStatuses()
        {
            var statuses = new List<string>
    {
        "Pending",
        "Confirmed",
        "Completed",
        "Canceled",
        "NoShow"
    };

            return Ok(statuses);
        }
    }
}