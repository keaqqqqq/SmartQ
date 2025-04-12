    // FNBReservation.Modules.Outlet.API/Controllers/AdminPeakHoursController.cs
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using FNBReservation.Modules.Outlet.Core.DTOs;
    using FNBReservation.Modules.Outlet.Core.Interfaces;

    namespace FNBReservation.Modules.Outlet.API.Controllers
    {
        [ApiController]
        [Route("api/v1/admin/outlets/{outletId}/peak-hours")]
        [Authorize(Policy = "AdminOnly")]
        public class AdminPeakHoursController : ControllerBase
        {
            private readonly IPeakHourService _peakHourService;
            private readonly ILogger<AdminPeakHoursController> _logger;

            public AdminPeakHoursController(
                IPeakHourService peakHourService,
                ILogger<AdminPeakHoursController> logger)
            {
                _peakHourService = peakHourService ?? throw new ArgumentNullException(nameof(peakHourService));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            [HttpPost]
            public async Task<IActionResult> CreatePeakHourSetting(Guid outletId, [FromBody] CreatePeakHourSettingDto createDto)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                try
                {
                    var userId = GetCurrentUserId();
                    var setting = await _peakHourService.CreatePeakHourSettingAsync(outletId, createDto, userId);
                    return CreatedAtAction(nameof(GetPeakHourSetting), new { outletId, peakHourId = setting.Id }, setting);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating peak hour setting for outlet: {OutletId}", outletId);
                    return StatusCode(500, new { message = "An error occurred while creating the peak hour setting" });
                }
            }

            [HttpGet]
            public async Task<IActionResult> GetAllPeakHourSettings(Guid outletId)
            {
                try
                {
                    var settings = await _peakHourService.GetPeakHourSettingsByOutletIdAsync(outletId);
                    return Ok(settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting peak hour settings for outlet: {OutletId}", outletId);
                    return StatusCode(500, new { message = "An error occurred while retrieving peak hour settings" });
                }
            }

            [HttpGet("active")]
            public async Task<IActionResult> GetActiveSettings(Guid outletId, [FromQuery] DateTime? date = null)
            {
                try
                {
                    var requestDate = date ?? DateTime.UtcNow.Date;
                    var settings = await _peakHourService.GetActivePeakHourSettingsAsync(outletId, requestDate);
                    return Ok(settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting active peak hour settings for outlet: {OutletId}", outletId);
                    return StatusCode(500, new { message = "An error occurred while retrieving active peak hour settings" });
                }
            }

            [HttpGet("{peakHourId}")]
            public async Task<IActionResult> GetPeakHourSetting(Guid outletId, Guid peakHourId)
            {
                try
                {
                    var setting = await _peakHourService.GetPeakHourSettingByIdAsync(peakHourId);

                    if (setting == null)
                        return NotFound(new { message = "Peak hour setting not found" });

                    if (setting.OutletId != outletId)
                        return BadRequest(new { message = "Peak hour setting does not belong to the specified outlet" });

                    return Ok(setting);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting peak hour setting: {PeakHourId} for outlet: {OutletId}", peakHourId, outletId);
                    return StatusCode(500, new { message = "An error occurred while retrieving the peak hour setting" });
                }
            }

            [HttpPut("{peakHourId}")]
            public async Task<IActionResult> UpdatePeakHourSetting(
                Guid outletId,
                Guid peakHourId,
                [FromBody] UpdatePeakHourSettingDto updateDto)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                try
                {
                    var userId = GetCurrentUserId();
                    var setting = await _peakHourService.GetPeakHourSettingByIdAsync(peakHourId);

                    if (setting == null)
                        return NotFound(new { message = "Peak hour setting not found" });

                    if (setting.OutletId != outletId)
                        return BadRequest(new { message = "Peak hour setting does not belong to the specified outlet" });

                    var updatedSetting = await _peakHourService.UpdatePeakHourSettingAsync(peakHourId, updateDto, userId);
                    return Ok(updatedSetting);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating peak hour setting: {PeakHourId} for outlet: {OutletId}", peakHourId, outletId);
                    return StatusCode(500, new { message = "An error occurred while updating the peak hour setting" });
                }
            }

            [HttpDelete("{peakHourId}")]
            public async Task<IActionResult> DeletePeakHourSetting(Guid outletId, Guid peakHourId)
            {
                try
                {
                    var setting = await _peakHourService.GetPeakHourSettingByIdAsync(peakHourId);

                    if (setting == null)
                        return NotFound(new { message = "Peak hour setting not found" });

                    if (setting.OutletId != outletId)
                        return BadRequest(new { message = "Peak hour setting does not belong to the specified outlet" });

                    var result = await _peakHourService.DeletePeakHourSettingAsync(peakHourId);
                    return Ok(new { message = "Peak hour setting deleted successfully" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting peak hour setting: {PeakHourId} for outlet: {OutletId}", peakHourId, outletId);
                    return StatusCode(500, new { message = "An error occurred while deleting the peak hour setting" });
                }
            }

            [HttpGet("current-allocation")]
            public async Task<IActionResult> GetCurrentAllocation(Guid outletId, [FromQuery] DateTime? dateTime = null)
            {
                try
                {
                    var requestDateTime = dateTime ?? DateTime.UtcNow;
                    var allocation = await _peakHourService.GetCurrentReservationAllocationAsync(outletId, requestDateTime);

                    return Ok(new
                    {
                        reservationAllocationPercent = allocation,
                        walkInAllocationPercent = 100 - allocation,
                        dateTime = requestDateTime
                    });
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting current allocation for outlet: {OutletId}", outletId);
                    return StatusCode(500, new { message = "An error occurred while retrieving the current allocation" });
                }
            }

            private Guid GetCurrentUserId()
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("User ID claim not found in token");
                    throw new InvalidOperationException("User ID claim not found in token");
                }

                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    _logger.LogWarning("Failed to parse user ID from claim: {UserIdClaim}", userIdClaim);
                    throw new InvalidOperationException($"Invalid user ID format in token: {userIdClaim}");
                }

                return userId;
            }
        }
    }