using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.DTOs;
using FNBReservation.Modules.Reservation.Core.Interfaces;

namespace FNBReservation.Modules.Reservation.API.Controllers
{
    [ApiController]
    [Route("api/v1/outlets/{outletId}/reservations")]
    [Authorize(Policy = "StaffOnly")]
    public class OutletReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly ILogger<OutletReservationController> _logger;

        public OutletReservationController(
            IReservationService reservationService,
            ILogger<OutletReservationController> logger)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetReservationsByOutlet(
            Guid outletId,
            [FromQuery] DateTime? date = null,
            [FromQuery] string status = null)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var reservations = await _reservationService.GetReservationsByOutletIdAsync(outletId, date, status);
                return Ok(reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving reservations" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation(Guid outletId, [FromBody] CreateReservationDto createReservationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Make sure the outlet ID matches the one in the route
            if (outletId != createReservationDto.OutletId)
            {
                return BadRequest(new { message = "Outlet ID in the request body does not match the one in the URL" });
            }

            // Make sure the outlet ID is valid for the user
            if (!await HasAccessToOutlet(outletId))
            {
                return Forbid();
            }

            try
            {
                var reservation = await _reservationService.CreateReservationAsync(createReservationDto);
                return CreatedAtAction(nameof(GetReservationById), new { outletId, id = reservation.Id }, reservation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while creating the reservation" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservationById(Guid outletId, Guid id)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var reservation = await _reservationService.GetReservationByIdAsync(id);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                // Make sure the reservation belongs to the specified outlet
                if (reservation.OutletId != outletId)
                    return BadRequest(new { message = "Reservation does not belong to the specified outlet" });

                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation: {ReservationId} for outlet: {OutletId}", id, outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving the reservation" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReservation(Guid outletId, Guid id, [FromBody] UpdateReservationDto updateReservationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                // Verify that the reservation belongs to this outlet
                var existingReservation = await _reservationService.GetReservationByIdAsync(id);
                if (existingReservation == null)
                    return NotFound(new { message = "Reservation not found" });

                if (existingReservation.OutletId != outletId)
                    return BadRequest(new { message = "Reservation does not belong to the specified outlet" });

                var reservation = await _reservationService.UpdateReservationAsync(id, updateReservationDto);
                return Ok(reservation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation: {ReservationId} for outlet: {OutletId}", id, outletId);
                return StatusCode(500, new { message = "An error occurred while updating the reservation" });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelReservation(Guid outletId, Guid id, [FromBody] CancelReservationDto cancelReservationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                // Verify that the reservation belongs to this outlet
                var existingReservation = await _reservationService.GetReservationByIdAsync(id);
                if (existingReservation == null)
                    return NotFound(new { message = "Reservation not found" });

                if (existingReservation.OutletId != outletId)
                    return BadRequest(new { message = "Reservation does not belong to the specified outlet" });

                var reservation = await _reservationService.CancelReservationAsync(id, cancelReservationDto);
                return Ok(reservation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling reservation: {ReservationId} for outlet: {OutletId}", id, outletId);
                return StatusCode(500, new { message = "An error occurred while canceling the reservation" });
            }
        }

        [HttpPut("{id}/no-show")]
        public async Task<IActionResult> MarkAsNoShow(Guid outletId, Guid id)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                // Verify that the reservation belongs to this outlet
                var existingReservation = await _reservationService.GetReservationByIdAsync(id);
                if (existingReservation == null)
                    return NotFound(new { message = "Reservation not found" });

                if (existingReservation.OutletId != outletId)
                    return BadRequest(new { message = "Reservation does not belong to the specified outlet" });

                var result = await _reservationService.MarkAsNoShowAsync(id);
                if (!result)
                    return BadRequest(new { message = "Failed to mark reservation as no-show" });

                return Ok(new { message = "Reservation marked as no-show successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reservation as no-show: {ReservationId} for outlet: {OutletId}", id, outletId);
                return StatusCode(500, new { message = "An error occurred while marking the reservation as no-show" });
            }
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> MarkAsCompleted(Guid outletId, Guid id)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                // Verify that the reservation belongs to this outlet
                var existingReservation = await _reservationService.GetReservationByIdAsync(id);
                if (existingReservation == null)
                    return NotFound(new { message = "Reservation not found" });

                if (existingReservation.OutletId != outletId)
                    return BadRequest(new { message = "Reservation does not belong to the specified outlet" });

                var result = await _reservationService.MarkAsCompletedAsync(id);
                if (!result)
                    return BadRequest(new { message = "Failed to mark reservation as completed" });

                return Ok(new { message = "Reservation marked as completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reservation as completed: {ReservationId} for outlet: {OutletId}", id, outletId);
                return StatusCode(500, new { message = "An error occurred while marking the reservation as completed" });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchReservations(
            [FromQuery] Guid outletId,
            [FromQuery] string searchTerm = "",
            [FromQuery] List<string> statuses = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                // Staff can only search within their assigned outlet
                var result = await _reservationService.SearchReservationsAsync(
                    new List<Guid> { outletId },
                    searchTerm,
                    statuses,
                    startDate,
                    endDate,
                    page,
                    pageSize,
                    isAdmin: false);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching reservations for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while searching reservations" });
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
        #region Helper Methods
        private Task<bool> HasAccessToOutlet(Guid outletId)
        {
            // Get the user's role from the claims
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Admins have access to all outlets
            if (role == "Admin")
                return Task.FromResult(true);

            // For OutletStaff, check if they belong to this outlet
            if (role == "OutletStaff")
            {
                // Get the user's OutletId claim
                var userOutletId = User.FindFirst("OutletId")?.Value;

                if (!string.IsNullOrEmpty(userOutletId) && Guid.TryParse(userOutletId, out Guid staffOutletId))
                {
                    // Check if the staff's outlet ID matches the requested outlet ID
                    return Task.FromResult(staffOutletId == outletId);
                }
            }

            // Default to no access
            return Task.FromResult(false);
        }
        #endregion
    }

}