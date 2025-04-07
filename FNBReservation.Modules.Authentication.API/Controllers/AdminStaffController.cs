// FNBReservation.Modules.Authentication.API/Controllers/AdminStaffController.cs (new file)
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FNBReservation.Modules.Authentication.Core.DTOs;
using FNBReservation.Modules.Authentication.Core.Interfaces;

namespace FNBReservation.Modules.Authentication.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/outlets/{outletId}/staff")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminStaffController : ControllerBase
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<AdminStaffController> _logger;

        public AdminStaffController(IStaffService staffService, ILogger<AdminStaffController> logger)
        {
            _staffService = staffService ?? throw new ArgumentNullException(nameof(staffService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(Guid outletId, [FromBody] CreateStaffDto createStaffDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ensure the outletId in the route matches the one in the DTO
            if (outletId != createStaffDto.OutletId)
            {
                return BadRequest(new { message = "Outlet ID in URL must match the one in the request body" });
            }

            try
            {
                var adminId = GetCurrentUserId();
                var staff = await _staffService.CreateStaffAsync(createStaffDto, adminId);
                return CreatedAtAction(nameof(GetStaff), new { outletId, staffId = staff.Id }, staff);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while creating the staff member" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStaff(Guid outletId)
        {
            try
            {
                var staffList = await _staffService.GetStaffByOutletIdAsync(outletId);
                return Ok(staffList);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving staff" });
            }
        }

        [HttpGet("{staffId}")]
        public async Task<IActionResult> GetStaff(Guid outletId, Guid staffId)
        {
            try
            {
                var staff = await _staffService.GetStaffByIdAsync(staffId);

                if (staff == null)
                    return NotFound(new { message = "Staff member not found" });

                // Ensure the staff belongs to the specified outlet
                if (staff.OutletId != outletId)
                    return BadRequest(new { message = "Staff member does not belong to the specified outlet" });

                return Ok(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff: {StaffId} for outlet: {OutletId}", staffId, outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving the staff member" });
            }
        }

        [HttpPut("{staffId}")]
        public async Task<IActionResult> UpdateStaff(Guid outletId, Guid staffId, [FromBody] UpdateStaffDto updateStaffDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // First, get the staff to check if it belongs to the outlet
                var existingStaff = await _staffService.GetStaffByIdAsync(staffId);
                if (existingStaff == null)
                    return NotFound(new { message = "Staff member not found" });

                // Ensure the staff belongs to the specified outlet
                if (existingStaff.OutletId != outletId)
                    return BadRequest(new { message = "Staff member does not belong to the specified outlet" });

                var adminId = GetCurrentUserId();
                var staff = await _staffService.UpdateStaffAsync(staffId, updateStaffDto, adminId);

                if (staff == null)
                    return NotFound(new { message = "Staff member not found" });

                return Ok(staff);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff: {StaffId} for outlet: {OutletId}", staffId, outletId);
                return StatusCode(500, new { message = "An error occurred while updating the staff member" });
            }
        }

        [HttpDelete("{staffId}")]
        public async Task<IActionResult> DeleteStaff(Guid outletId, Guid staffId)
        {
            try
            {
                // First, get the staff to check if it belongs to the outlet
                var existingStaff = await _staffService.GetStaffByIdAsync(staffId);
                if (existingStaff == null)
                    return NotFound(new { message = "Staff member not found" });

                // Ensure the staff belongs to the specified outlet
                if (existingStaff.OutletId != outletId)
                    return BadRequest(new { message = "Staff member does not belong to the specified outlet" });

                var result = await _staffService.DeleteStaffAsync(staffId);

                if (!result)
                    return NotFound(new { message = "Staff member not found" });

                return Ok(new { message = "Staff member deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff: {StaffId} for outlet: {OutletId}", staffId, outletId);
                return StatusCode(500, new { message = "An error occurred while deleting the staff member" });
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