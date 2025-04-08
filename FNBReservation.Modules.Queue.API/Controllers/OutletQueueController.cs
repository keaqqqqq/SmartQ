// FNBReservation.Modules.Queue.API/Controllers/OutletQueueController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FNBReservation.Modules.Queue.Core.DTOs;
using FNBReservation.Modules.Queue.Core.Interfaces;

namespace FNBReservation.Modules.Queue.API.Controllers
{
    [ApiController]
    [Route("api/v1/outlets/{outletId}/queue")]
    [Authorize(Policy = "StaffOnly")]
    public class OutletQueueController : ControllerBase
    {
        private readonly IQueueService _queueService;
        private readonly ILogger<OutletQueueController> _logger;

        public OutletQueueController(
            IQueueService queueService,
            ILogger<OutletQueueController> logger)
        {
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetQueueEntries(
            Guid outletId,
            [FromQuery] List<string> statuses = null,
            [FromQuery] string searchTerm = null,
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

                var entries = await _queueService.GetQueueEntriesAsync(
                    outletId, statuses, searchTerm, page, pageSize);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue entries for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving queue entries" });
            }
        }

        [HttpGet("waiting")]
        public async Task<IActionResult> GetWaitingEntries(Guid outletId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var entries = await _queueService.GetQueueEntriesByOutletIdAsync(outletId, "Waiting");
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting waiting queue entries for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving waiting queue entries" });
            }
        }

        [HttpGet("held")]
        public async Task<IActionResult> GetHeldEntries(Guid outletId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var entries = await _queueService.GetHeldEntriesAsync(outletId);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting held queue entries for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving held queue entries" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetQueueSummary(Guid outletId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var summary = await _queueService.GetQueueSummaryAsync(outletId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue summary for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while retrieving queue summary" });
            }
        }

        [HttpPost("call-next")]
        public async Task<IActionResult> CallNextCustomer(
            Guid outletId,
            [FromBody] CallNextCustomerDto callNextDto)
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

                var staffId = GetCurrentUserId();
                var queueEntry = await _queueService.CallNextCustomerAsync(outletId, callNextDto.TableId, staffId);
                return Ok(queueEntry);
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
                _logger.LogError(ex, "Error calling next customer for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while calling the next customer" });
            }
        }

        [HttpGet("table-recommendation/{tableId}")]
        public async Task<IActionResult> GetTableRecommendation(Guid outletId, Guid tableId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var recommendation = await _queueService.GetTableRecommendationAsync(outletId, tableId);
                if (recommendation == null)
                    return NotFound(new { message = "No recommendations available" });

                return Ok(recommendation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table recommendation for outlet: {OutletId}, table: {TableId}", outletId, tableId);
                return StatusCode(500, new { message = "An error occurred while getting table recommendation" });
            }
        }

        [HttpPost("assign-table")]
        public async Task<IActionResult> AssignTable(
            Guid outletId,
            [FromBody] AssignTableDto assignTableDto)
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

                // Ensure staff ID is set to current user
                assignTableDto.StaffId = GetCurrentUserId();

                var queueEntry = await _queueService.AssignTableToQueueEntryAsync(assignTableDto.QueueEntryId, assignTableDto);
                return Ok(queueEntry);
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
                _logger.LogError(ex, "Error assigning table for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while assigning the table" });
            }
        }

        [HttpPost("{queueEntryId}/seated")]
        public async Task<IActionResult> MarkAsSeated(Guid outletId, Guid queueEntryId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var staffId = GetCurrentUserId();
                var queueEntry = await _queueService.MarkQueueEntryAsSeatedAsync(queueEntryId, staffId);
                return Ok(queueEntry);
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
                _logger.LogError(ex, "Error marking queue entry as seated: {QueueEntryId} for outlet: {OutletId}", queueEntryId, outletId);
                return StatusCode(500, new { message = "An error occurred while marking the queue entry as seated" });
            }
        }

        [HttpPost("{queueEntryId}/completed")]
        public async Task<IActionResult> MarkAsCompleted(Guid outletId, Guid queueEntryId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var staffId = GetCurrentUserId();
                var queueEntry = await _queueService.MarkQueueEntryAsCompletedAsync(queueEntryId, staffId);
                return Ok(queueEntry);
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
                _logger.LogError(ex, "Error marking queue entry as completed: {QueueEntryId} for outlet: {OutletId}", queueEntryId, outletId);
                return StatusCode(500, new { message = "An error occurred while marking the queue entry as completed" });
            }
        }

        [HttpPost("{queueEntryId}/no-show")]
        public async Task<IActionResult> MarkAsNoShow(Guid outletId, Guid queueEntryId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var staffId = GetCurrentUserId();
                var queueEntry = await _queueService.MarkQueueEntryAsNoShowAsync(queueEntryId, staffId);
                return Ok(queueEntry);
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
                _logger.LogError(ex, "Error marking queue entry as no-show: {QueueEntryId} for outlet: {OutletId}", queueEntryId, outletId);
                return StatusCode(500, new { message = "An error occurred while marking the queue entry as no-show" });
            }
        }

        [HttpPost("{queueEntryId}/cancel")]
        public async Task<IActionResult> CancelQueueEntry(Guid outletId, Guid queueEntryId, [FromBody] CancelQueueEntryDto cancelDto)
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

                var staffId = GetCurrentUserId();
                var result = await _queueService.CancelQueueEntryAsync(queueEntryId, cancelDto.Reason, staffId);
                if (!result)
                    return NotFound(new { message = "Queue entry not found" });

                return Ok(new { message = "Queue entry cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling queue entry: {QueueEntryId} for outlet: {OutletId}", queueEntryId, outletId);
                return StatusCode(500, new { message = "An error occurred while cancelling the queue entry" });
            }
        }

        [HttpPost("{queueEntryId}/prioritize")]
        public async Task<IActionResult> PrioritizeHeldEntry(Guid outletId, Guid queueEntryId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                var staffId = GetCurrentUserId();
                var queueEntry = await _queueService.PrioritizeHeldEntryAsync(queueEntryId, staffId);
                return Ok(queueEntry);
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
                _logger.LogError(ex, "Error prioritizing held entry: {QueueEntryId} for outlet: {OutletId}", queueEntryId, outletId);
                return StatusCode(500, new { message = "An error occurred while prioritizing the held entry" });
            }
        }

        [HttpPost("reorder")]
        public async Task<IActionResult> ReorderQueue(Guid outletId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                await _queueService.ReorderQueueAsync(outletId);
                return Ok(new { message = "Queue reordered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering queue for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while reordering the queue" });
            }
        }

        [HttpPost("update-wait-times")]
        public async Task<IActionResult> UpdateWaitTimes(Guid outletId)
        {
            try
            {
                // Make sure the outlet ID is valid for the user
                if (!await HasAccessToOutlet(outletId))
                {
                    return Forbid();
                }

                await _queueService.UpdateWaitTimesAsync(outletId);
                return Ok(new { message = "Wait times updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wait times for outlet: {OutletId}", outletId);
                return StatusCode(500, new { message = "An error occurred while updating wait times" });
            }
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
        #endregion
    }
}