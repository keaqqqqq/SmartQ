using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Reservation.Core.DTOs;
using FNBReservation.Modules.Reservation.Core.Interfaces;
using System.Runtime.InteropServices;

namespace FNBReservation.Modules.Reservation.API.Controllers
{
    [ApiController]
    [Route("api/v1/reservations")]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            IReservationService reservationService,
            ILogger<ReservationController> logger)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("check-availability")]
        public async Task<IActionResult> CheckAvailability([FromBody] CheckAvailabilityRequestDto request)
        {
            _logger.LogInformation("Received check availability request for outlet: {OutletId}", request.OutletId);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var availability = await _reservationService.CheckAvailabilityAsync(request);
                return Ok(availability);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for outlet: {OutletId}", request.OutletId);
                return StatusCode(500, new { message = "An error occurred while checking availability" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto createReservationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var reservation = await _reservationService.CreateReservationAsync(createReservationDto);
                return CreatedAtAction(nameof(GetReservationById), new { id = reservation.Id }, reservation);
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
                _logger.LogError(ex, "Error creating reservation for outlet: {OutletId}", createReservationDto.OutletId);
                return StatusCode(500, new { message = "An error occurred while creating the reservation" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservationById(Guid id)
        {
            try
            {
                var reservation = await _reservationService.GetReservationByIdAsync(id);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation: {ReservationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the reservation" });
            }
        }

        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetReservationByCode(string code)
        {
            try
            {
                var reservation = await _reservationService.GetReservationByCodeAsync(code);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation by code: {ReservationCode}", code);
                return StatusCode(500, new { message = "An error occurred while retrieving the reservation" });
            }
        }

        [HttpGet("phone/{phone}")]
        public async Task<IActionResult> GetReservationsByPhone(string phone)
        {
            try
            {
                var reservations = await _reservationService.GetReservationsByPhoneAsync(phone);
                return Ok(reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservations for phone: {Phone}", phone);
                return StatusCode(500, new { message = "An error occurred while retrieving reservations" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReservation(Guid id, [FromBody] UpdateReservationDto updateReservationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var reservation = await _reservationService.UpdateReservationAsync(id, updateReservationDto);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

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
                _logger.LogError(ex, "Error updating reservation: {ReservationId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the reservation" });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelReservation(Guid id, [FromBody] CancelReservationDto cancelReservationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var reservation = await _reservationService.CancelReservationAsync(id, cancelReservationDto);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                return Ok(reservation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling reservation: {ReservationId}", id);
                return StatusCode(500, new { message = "An error occurred while canceling the reservation" });
            }
        }

        [HttpPost("hold-tables")]
        public async Task<IActionResult> HoldTablesForReservation([FromBody] TableHoldRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _reservationService.HoldTablesForReservationAsync(request);

                if (!result.IsSuccessful)
                    return BadRequest(new { message = result.ErrorMessage });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error holding tables for reservation");
                return StatusCode(500, new { message = "An error occurred while holding tables" });
            }
        }

        [HttpPost("release-hold/{holdId}")]
        public async Task<IActionResult> ReleaseTableHold(Guid holdId)
        {
            try
            {
                var result = await _reservationService.ReleaseTableHoldAsync(holdId);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing table hold {HoldId}", holdId);
                return StatusCode(500, new { message = "An error occurred while releasing the table hold" });
            }
        }

        [HttpPut("update-hold-time")]
        public async Task<IActionResult> UpdateHoldTime([FromBody] UpdateHoldTimeRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _reservationService.UpdateTableHoldTimeAsync(request);

                if (!result.IsSuccessful)
                    return BadRequest(new { message = result.ErrorMessage });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating hold time");
                return StatusCode(500, new { message = "An error occurred while updating the hold time" });
            }
        }

        [HttpGet("alternative-times")]
        public async Task<IActionResult> GetAlternativeTimeSlots(
            [FromQuery] Guid outletId,
            [FromQuery] DateTime referenceTime,
            [FromQuery] int partySize,
            [FromQuery] int rangeMinutes = 30)
        {
            try
            {
                var timeSlots = await _reservationService.GetAlternativeTimeSlotsAsync(
                    outletId, referenceTime, partySize, rangeMinutes);

                return Ok(timeSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alternative time slots");
                return StatusCode(500, new { message = "An error occurred while retrieving alternative time slots" });
            }
        }
    }
}