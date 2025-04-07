// FNBReservation.Modules.Outlet.API/Controllers/AdminOutletController.cs
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FNBReservation.Modules.Outlet.Core.DTOs;
using FNBReservation.Modules.Outlet.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FNBReservation.Modules.Outlet.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/outlets")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminOutletController : ControllerBase
    {
        private readonly IOutletService _outletService;
        private readonly ITableService _tableService;
        private readonly IPeakHourService _peakHourService;
        private readonly ILogger<AdminOutletController> _logger;

        public AdminOutletController(IOutletService outletService, ITableService tableService,
            IPeakHourService peakHourService, ILogger<AdminOutletController> logger)
        {
            _outletService = outletService ?? throw new ArgumentNullException(nameof(outletService));
            _tableService = tableService ?? throw new ArgumentNullException(nameof(tableService));
            _peakHourService = peakHourService ?? throw new ArgumentNullException(nameof(peakHourService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOutlet([FromBody] CreateOutletDto createOutletDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var outlet = await _outletService.CreateOutletAsync(createOutletDto, userId);
                return CreatedAtAction(nameof(GetOutlet), new { id = outlet.Id }, outlet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating outlet");
                return StatusCode(500, new { message = "An error occurred while creating the outlet" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOutlet(Guid id)
        {
            try
            {
                var outlet = await _outletService.GetOutletByIdAsync(id);

                if (outlet == null)
                    return NotFound(new { message = "Outlet not found" });

                // Get peak hour settings for this outlet
                var peakHourSettings = await _peakHourService.GetPeakHourSettingsByOutletIdAsync(id);

                // Include the peak hour settings in the response
                outlet.PeakHourSettings = peakHourSettings.ToList();

                return Ok(outlet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outlet by ID: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the outlet" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOutlets()
        {
            try
            {
                var outlets = await _outletService.GetAllOutletsAsync();
                return Ok(outlets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all outlets");
                return StatusCode(500, new { message = "An error occurred while retrieving outlets" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOutlet(Guid id, [FromBody] UpdateOutletDto updateOutletDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var outlet = await _outletService.UpdateOutletAsync(id, updateOutletDto, userId);

                if (outlet == null)
                    return NotFound(new { message = "Outlet not found" });

                return Ok(outlet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating outlet: {OutletId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the outlet" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOutlet(Guid id)
        {
            try
            {
                var result = await _outletService.DeleteOutletAsync(id);

                if (!result)
                    return NotFound(new { message = "Outlet not found" });

                return Ok(new { message = "Outlet deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting outlet: {OutletId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the outlet" });
            }
        }

        [HttpGet("{id}/changes")]
        public async Task<IActionResult> GetOutletChanges(Guid id)
        {
            try
            {
                var changes = await _outletService.GetOutletChangesAsync(id);
                return Ok(changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting changes for outlet: {OutletId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving outlet changes" });
            }
        }

        [HttpPut("changes/{id}")]
        public async Task<IActionResult> RespondToOutletChange(Guid id, [FromBody] OutletChangeResponseDto responseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var change = await _outletService.RespondToOutletChangeAsync(id, responseDto, userId);

                if (change == null)
                    return NotFound(new { message = "Outlet change not found or cannot be updated" });

                return Ok(change);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to outlet change: {ChangeId}", id);
                return StatusCode(500, new { message = "An error occurred while processing the outlet change" });
            }
        }

        [HttpGet("{id}/summary")]
        public async Task<IActionResult> GetOutletSummary(Guid id)
        {
            try
            {
                // Get the outlet
                var outlet = await _outletService.GetOutletByIdAsync(id);
                if (outlet == null)
                    return NotFound(new { message = "Outlet not found" });

                // Get tables and sections summary
                var sections = await _tableService.GetSectionsByOutletIdAsync(id);
                var totalCapacity = await _tableService.GetTotalTablesCapacityAsync(id);
                var reservationCapacity = await _tableService.GetReservationCapacityAsync(id);

                // Get active peak hour settings
                var today = DateTime.UtcNow.Date;
                var activePeakHours = await _peakHourService.GetActivePeakHourSettingsAsync(id, today);

                // Get Ramadan settings
                var ramadanSettings = await _peakHourService.GetRamadanSettingsByOutletIdAsync(id);

                // Build response
                var summary = new
                {
                    outlet = outlet,
                    sections = sections,
                    capacity = new
                    {
                        total = totalCapacity,
                        reservation = reservationCapacity,
                        walkIn = totalCapacity - reservationCapacity,
                        reservationPercentage = outlet.ReservationAllocationPercent,
                        walkInPercentage = 100 - outlet.ReservationAllocationPercent
                    },
                    activePeakHours = activePeakHours,
                    ramadanMode = ramadanSettings.Any(s => s.IsActive &&
                                                     s.RamadanStartDate.HasValue &&
                                                     s.RamadanEndDate.HasValue &&
                                                     today >= s.RamadanStartDate.Value.Date &&
                                                     today <= s.RamadanEndDate.Value.Date)
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting summary for outlet: {OutletId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the outlet summary" });
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